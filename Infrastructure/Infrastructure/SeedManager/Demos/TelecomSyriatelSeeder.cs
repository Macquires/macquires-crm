using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.Suspension;
using Application.Features.NumberSequenceManager;
using Application.Features.TelecomManager.Commands;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Subscriber 360° demo: hero «سعدون الشامي», bulk subscribers, several CRM parties with multiple
/// <see cref="SubscriberProfile"/> (B2B / family), plus demo MGR operation.
/// </summary>
public class TelecomSyriatelSeeder
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ICommandRepository<BillingIntegrationLog> _logRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFieldEncryptionService _encryption;

    public TelecomSyriatelSeeder(
        IQueryContext query,
        ICommandRepository<Customer> customerRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<BillingIntegrationLog> logRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork,
        IFieldEncryptionService encryption)
    {
        _query = query;
        _customerRepository = customerRepository;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _subscriptionRepository = subscriptionRepository;
        _simRepository = simRepository;
        _operationRepository = operationRepository;
        _logRepository = logRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
        _encryption = encryption;
    }

    private static string AdjustMsisdnForHlrAlignment(string msisdn, SubscriberOperationalStatus status, Random rnd)
    {
        if (string.IsNullOrWhiteSpace(msisdn) || msisdn.Length < 2 || IsWellKnownDemoMsisdn(msisdn)) return msisdn;
        
        // Special case: Operational suspension (last digit 5)
        if (status is SubscriberOperationalStatus.Suspended or SubscriberOperationalStatus.SuspendedInbound or SubscriberOperationalStatus.SuspendedOutbound)
        {
            return $"{msisdn[..^1]}5";
        }

        var prefix = msisdn[..^1];
        var lastDigit = status switch
        {
            SubscriberOperationalStatus.Active => rnd.Next(4) switch
            {
                0 => 0,
                1 => 2,
                2 => 6,
                _ => 8
            },
            SubscriberOperationalStatus.Terminated => 1,
            _ => rnd.Next(10)
        };
        return $"{prefix}{lastDigit}";
    }

    private async Task CreateIndividualAsync(IndividualCustomer customer)
    {
        customer.SyncNationalIdSearchHash(_encryption);
        await _customerRepository.CreateAsync(customer);
    }

    /// <summary>
    /// Backfills SIM kit (ICCID/IMSI) for active demo lines that have MSISDN but no linked <see cref="SimInventory"/>.
    /// </summary>
    public async Task EnsureDemoSimKitsAsync()
    {
        var rows = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.MsisdnAssetId != null)
            .Select(s => new
            {
                s.SubscriberProfileId,
                MsisdnAssetId = s.MsisdnAssetId!,
            })
            .ToListAsync();

        if (rows.Count == 0)
        {
            return;
        }

        var profileIds = rows.Select(r => r.SubscriberProfileId).Distinct().ToList();
        var profilesWithSim = await _query.SimInventory.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfileId != null && profileIds.Contains(s.SubscriberProfileId!))
            .Select(s => s.SubscriberProfileId!)
            .Distinct()
            .ToListAsync();
        var profilesWithSimSet = profilesWithSim.ToHashSet(StringComparer.Ordinal);

        var assetIds = rows.Select(r => r.MsisdnAssetId).Distinct().ToList();
        var assets = await _query.MsisdnAsset.IsDeletedEqualTo()
            .Where(a => assetIds.Contains(a.Id))
            .ToListAsync();

        foreach (var asset in assets)
        {
            var profileId = rows.FirstOrDefault(r => r.MsisdnAssetId == asset.Id)?.SubscriberProfileId;
            if (string.IsNullOrEmpty(profileId) || profilesWithSimSet.Contains(profileId))
            {
                continue;
            }

            var iccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(asset.Msisdn);
            if (string.IsNullOrWhiteSpace(iccid))
            {
                continue;
            }

            var existing = await _query.SimInventory.AsNoTracking()
                .FirstOrDefaultAsync(s => !s.IsDeleted && s.Iccid == iccid);
            if (existing != null)
            {
                if (string.IsNullOrEmpty(existing.SubscriberProfileId))
                {
                    var tracked = await _simRepository.GetAsync(existing.Id, CancellationToken.None);
                    if (tracked != null)
                    {
                        tracked.AssignToProfile(profileId);
                        if (tracked.Status == SimStatus.Available)
                        {
                            tracked.TransitionTo(SimStatus.Active);
                        }
                    }
                }

                await PairMsisdnKitAsync(asset.Id, iccid, MsisdnAssetKitResolver.DeriveImsiFromMsisdn(asset.Msisdn));
                profilesWithSimSet.Add(profileId);
                continue;
            }

            var imsi = MsisdnAssetKitResolver.DeriveImsiFromMsisdn(asset.Msisdn);
            var sim = SimInventory.Create(iccid, imsi: imsi);
            sim.AssignToProfile(profileId);
            sim.TransitionTo(SimStatus.Active);
            await _simRepository.CreateAsync(sim);

            await PairMsisdnKitAsync(asset.Id, iccid, imsi);
            profilesWithSimSet.Add(profileId);
        }

        await _unitOfWork.SaveAsync();
    }

    /// <summary>
    /// Inventory SIM rows are provisioned exclusively via Oracle Fusion SCM sync (zero-coupling at warehouse level).
    /// </summary>
    public Task EnsureAvailablePoolSimKitsAsync() => Task.CompletedTask;

    /// <summary>
    /// Idempotent backfill for existing demo DBs: hero line bound to MIX_500 + completed MGR/VAS for KPIs.
    /// </summary>
    public async Task EnsureHeroOfferSubscriptionDemoAsync()
    {
        var mixOffering = await _query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == "MIX_500")
            .Select(o => new { o.Id, o.ProductId, o.Name })
            .FirstOrDefaultAsync();

        var yaHalaOffering = await _query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == "YA_HALA_30")
            .Select(o => new { o.Id, o.ProductId })
            .FirstOrDefaultAsync();

        if (mixOffering == null)
        {
            return;
        }

        var heroAsset = await _query.MsisdnAsset.AsNoTracking()
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.Msisdn == TelecomDemoMsisdn.Hero);

        if (heroAsset == null || string.IsNullOrEmpty(heroAsset.SubscriberProfileId))
        {
            return;
        }

        var subscription = await _query.TelecomSubscription.IsDeletedEqualTo()
            .Where(s => s.MsisdnAssetId == heroAsset.Id)
            .OrderByDescending(s => s.IsPrimaryLine)
            .ThenBy(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (subscription != null
            && (subscription.ProductOfferingId != mixOffering.Id
                || (mixOffering.ProductId != null && subscription.ProductId != mixOffering.ProductId)))
        {
            var tracked = await _subscriptionRepository.GetAsync(subscription.Id, CancellationToken.None);
            if (tracked != null)
            {
                tracked.ProductOfferingId = mixOffering.Id;
                if (mixOffering.ProductId != null)
                {
                    tracked.ProductId = mixOffering.ProductId;
                }

                _subscriptionRepository.Update(tracked);
                await _unitOfWork.SaveAsync();
            }
        }

        var demoNow = DateTime.UtcNow;
        var mgr = await _query.TelecomOperationRequest.IsDeletedEqualTo()
            .Where(o => o.Kind == TelecomOperationKind.Migration
                        && o.SubscriberProfileId == heroAsset.SubscriberProfileId
                        && (o.MsisdnAssetId == null || o.MsisdnAssetId == heroAsset.Id))
            .OrderByDescending(o => o.CreatedAtUtc)
            .FirstOrDefaultAsync();

        if (mgr != null && mgr.Status != TelecomOperationStatus.Completed)
        {
            var trackedMgr = await _operationRepository.GetAsync(mgr.Id, CancellationToken.None);
            if (trackedMgr != null)
            {
                trackedMgr.Status = TelecomOperationStatus.Completed;
                trackedMgr.DocumentStatus = TelecomDocumentStatus.Verified;
                trackedMgr.ProductOfferingId = mixOffering.Id;
                trackedMgr.ProductId = mixOffering.ProductId;
                trackedMgr.PriorProductOfferingId = yaHalaOffering?.Id;
                trackedMgr.PriorProductId = yaHalaOffering?.ProductId;
                trackedMgr.TargetOfferName = mixOffering.Name;
                trackedMgr.ConfirmedAtUtc ??= demoNow.AddMinutes(8);
                trackedMgr.CreatedAtUtc ??= demoNow.AddMinutes(-12);
                trackedMgr.Notes = "ديمو MGR مكتمل: يا هلا → سيريتل ميكس 500 — KPIs وعرض 360.";
                _operationRepository.Update(trackedMgr);
                await _unitOfWork.SaveAsync();
            }
        }
        else if (mgr == null)
        {
            var (entityName, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.Migration);
            var op = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.Migration,
                Number = _numberSequenceService.GenerateNumber(entityName, prefix, "", useDate: false),
                Status = TelecomOperationStatus.Completed,
                DocumentStatus = TelecomDocumentStatus.Verified,
                SubscriberProfileId = heroAsset.SubscriberProfileId,
                MsisdnAssetId = heroAsset.Id,
                ProductOfferingId = mixOffering.Id,
                ProductId = mixOffering.ProductId,
                PriorProductOfferingId = yaHalaOffering?.Id,
                PriorProductId = yaHalaOffering?.ProductId,
                TargetOfferName = mixOffering.Name,
                ConfirmedAtUtc = demoNow.AddMinutes(8),
                CreatedAtUtc = demoNow.AddMinutes(-12),
                Notes = "ديمو MGR مكتمل: يا هلا → سيريتل ميكس 500 — KPIs وعرض 360.",
            };
            await _operationRepository.CreateAsync(op);
            await _unitOfWork.SaveAsync();
        }

        var hasVasDemo = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo(false)
            .AnyAsync(o => o.Kind == TelecomOperationKind.ServiceModification
                           && o.SubscriberProfileId == heroAsset.SubscriberProfileId
                           && o.Notes != null
                           && o.Notes.Contains("Activate VAS VAS_CALLER_ID"));

        if (!hasVasDemo)
        {
            var (vasEntity, vasPrefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.ServiceModification);
            await _operationRepository.CreateAsync(new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.ServiceModification,
                Number = _numberSequenceService.GenerateNumber(vasEntity, vasPrefix, "", useDate: false),
                Status = TelecomOperationStatus.Completed,
                DocumentStatus = TelecomDocumentStatus.Verified,
                SubscriberProfileId = heroAsset.SubscriberProfileId,
                MsisdnAssetId = heroAsset.Id,
                CreatedAtUtc = demoNow.AddMinutes(-5),
                Notes = "Activate VAS VAS_CALLER_ID — ديمو كاشف الأرقام.",
            });
            await _unitOfWork.SaveAsync();
        }
    }

    private async Task PairMsisdnKitAsync(string msisdnAssetId, string iccid, string? imsi)
    {
        var tracked = await _msisdnRepository.GetAsync(msisdnAssetId, CancellationToken.None);
        if (tracked == null)
        {
            return;
        }

        tracked.PairedIccid = iccid;
        tracked.PairedImsi = imsi;
        // Entity is already tracked from GetAsync — avoid Update() which would force UPDATE semantics incorrectly.
    }

    private static bool IsWellKnownDemoMsisdn(string msisdn) => TelecomDemoMsisdn.IsWellKnown(msisdn);

    private static string ResolvePrimaryMsisdn(Customer cust, Random rnd)
    {
        if (cust.DisplayName.Contains(TelecomDemoMsisdn.ShowcaseCustomerName, StringComparison.Ordinal))
        {
            return TelecomDemoMsisdn.Hero;
        }

        if (!string.IsNullOrWhiteSpace(cust.PrimaryPhone))
        {
            return TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(cust.PrimaryPhone)
                ?? cust.PrimaryPhone.Replace(" ", "").Trim();
        }

        return $"093{Math.Abs(cust.Id.GetHashCode()) % 10_000_000:0000000}";
    }

    private sealed class MsisdnPoolCatalog
    {
        public List<string> TypeIds { get; }
        private readonly Dictionary<string, List<string>> _productsByType;

        public MsisdnPoolCatalog(List<string> typeIds, Dictionary<string, List<string>> productsByType)
        {
            TypeIds = typeIds;
            _productsByType = productsByType;
        }

        public (string TypeId, string? ProductId) PickForIndex(int index, Random rnd)
        {
            var typeId = TypeIds[(index - 1) % TypeIds.Count];
            return (typeId, PickProductForType(typeId, rnd));
        }

        public string? PickProductForType(string typeId, Random rnd)
        {
            if (!_productsByType.TryGetValue(typeId, out var products) || products.Count == 0)
            {
                return null;
            }

            return products[rnd.Next(products.Count)];
        }
    }

    private async Task<MsisdnPoolCatalog> LoadMsisdnPoolCatalogAsync()
    {
        var typeIds = await _query.TelecomSubscriptionTypeLookup.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.Id)
            .ToListAsync();

        var products = await _query.Product.AsNoTracking()
            .Where(p => !p.IsDeleted && p.Physical != true && p.CompatibleSubscriptionTypeId != null)
            .Select(p => new { p.Id, p.CompatibleSubscriptionTypeId })
            .ToListAsync();

        var byType = products
            .GroupBy(p => p.CompatibleSubscriptionTypeId!)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        return new MsisdnPoolCatalog(typeIds, byType);
    }

    /// <summary>Demo quarantine rows — Available/Reserved must pass through Active per pool state machine.</summary>
    private static void ApplyDemoQuarantineState(MsisdnAsset asset, DateTime quarantineEndsUtc)
    {
        if (asset.PoolStatus is MsisdnPoolStatus.Available or MsisdnPoolStatus.Reserved)
        {
            asset.TransitionTo(MsisdnPoolStatus.Active);
        }

        if (asset.PoolStatus != MsisdnPoolStatus.Quarantined)
        {
            asset.TransitionTo(MsisdnPoolStatus.Quarantined);
        }

        asset.QuarantineEndsUtc = quarantineEndsUtc;
    }

    private async Task ApplyDemoInventoryStatePatchesAsync(MsisdnPoolCatalog poolCatalog, Random rnd)
    {
        var quarantineOffsets = new[] { -5, 10, 30 };
        for (var i = 0; i < 3; i++)
        {
            var num = $"093555000{i + 1}";
            var asset = await _msisdnRepository.GetQuery()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Msisdn == num);
            if (asset == null || !string.IsNullOrEmpty(asset.SubscriberProfileId))
            {
                continue;
            }

            var (typeId, productId) = poolCatalog.PickForIndex(i + 101, rnd);
            asset.IntendedSubscriptionTypeId = typeId;
            asset.ProductId = productId;
            
            // GLOBAL INVENTORY & PAIRING NORMALIZATION: Quarantined assets must have zero-coupling
            asset.SubscriberProfileId = null;
            asset.PairedIccid = null;
            asset.PairedImsi = null;

            ApplyDemoQuarantineState(asset, DateTime.UtcNow.AddDays(quarantineOffsets[i]));
        }

        for (var i = 0; i < 5; i++)
        {
            var num = $"093999000{i + 1}";
            var asset = await _msisdnRepository.GetQuery()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Msisdn == num);
            if (asset == null || !string.IsNullOrEmpty(asset.SubscriberProfileId))
            {
                continue;
            }

            var (typeId, productId) = poolCatalog.PickForIndex(i + 201, rnd);
            asset.IntendedSubscriptionTypeId = typeId;
            asset.ProductId = productId;
            asset.ReservedUntilUtc = i < 3
                ? DateTime.UtcNow.AddHours(-2)
                : DateTime.UtcNow.AddHours(12);
            asset.ReservedForCustomerId = null;

            // GLOBAL INVENTORY & PAIRING NORMALIZATION: Reserved assets in pool must have zero-coupling
            asset.SubscriberProfileId = null;
            asset.PairedIccid = null;
            asset.PairedImsi = null;

            if (asset.PoolStatus != MsisdnPoolStatus.Reserved)
            {
                asset.TransitionTo(MsisdnPoolStatus.Reserved);
            }
        }

        for (var i = 1; i <= 100; i++)
        {
            var pfx = i % 2 == 0 ? "093" : "099";
            var num = $"{pfx}{i + 2_000_000:0000000}";
            var asset = await _msisdnRepository.GetQuery()
                .FirstOrDefaultAsync(m => !m.IsDeleted && m.Msisdn == num);
            if (asset == null || !string.IsNullOrEmpty(asset.SubscriberProfileId))
            {
                continue;
            }

            var (typeId, productId) = poolCatalog.PickForIndex(i, rnd);
            asset.IntendedSubscriptionTypeId = typeId;
            asset.ProductId = productId;

            // GLOBAL INVENTORY & PAIRING NORMALIZATION: Available assets must have zero-coupling
            if (asset.PoolStatus == MsisdnPoolStatus.Available)
            {
                asset.ResetToAvailableForInventoryIngest();
            }
        }

        await _unitOfWork.SaveAsync();
    }

    /// <summary>Allocates a Syrian-style MSISDN not already present on a non-deleted <see cref="MsisdnAsset"/>.</summary>
    private async Task<string> GenerateUniqueDemoMsisdnAsync(Random rnd)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            var candidate = $"09{rnd.Next(3, 10)}{rnd.Next(1000000, 9999999)}";
            var taken = await _query.MsisdnAsset.AsNoTracking().AnyAsync(m => !m.IsDeleted && m.Msisdn == candidate);
            if (!taken)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("TelecomSyriatelSeeder: could not allocate a unique demo MSISDN.");
    }

    public async Task GenerateDataAsync()
    {
        var poolCatalog = await LoadMsisdnPoolCatalogAsync();
        if (poolCatalog.TypeIds.Count == 0)
            return;

        var rnd = new Random(20260512);
        await ApplyDemoInventoryStatePatchesAsync(poolCatalog, rnd);

        if (await _query.SubscriberProfile.AnyAsync())
            return;

        var groups = await _query.CustomerGroup.AsNoTracking().Select(x => x.Id).ToListAsync();
        var categories = await _query.CustomerCategory.AsNoTracking().Select(x => x.Id).ToListAsync();

        var heroCustomer = IndividualCustomer.Create(
            "سعدون الشامي",
            _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST"),
            "0109988776",
            new PostalAddress("مشروع دمر — سكن جديد", "دمشق", "دمشق", "22000", "سوريا"),
            "sadoun.alshami@syriatel-demo.local",
            TelecomDemoMsisdn.Hero,
            groups[rnd.Next(groups.Count)],
            categories[rnd.Next(categories.Count)],
            description: "عميل منذ نحو 10 سنوات — سيناريو ديمو.");
        await CreateIndividualAsync(heroCustomer);

        var debtCustomer = IndividualCustomer.Create(
            "مازن المديون",
            _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST"),
            "0108877665",
            new PostalAddress("دمشق القديمة", "دمشق", "دمشق", "00000", "سوريا"),
            "mazen.demo@syriatel-demo.local",
            TelecomDemoMsisdn.DebtSubscriber,
            groups[rnd.Next(groups.Count)],
            categories[rnd.Next(categories.Count)]);
        debtCustomer.SetDescription("مشترك عليه ديون — سيناريو TakeOver (خط منفصل عن عرض الاستكشاف).");
        await CreateIndividualAsync(debtCustomer);

        var multiProfileParties = new List<(string Name, int ProfileCount, string City, string Street, string Phone)>
        {
            ("البنك العربي — ديمو تعدد ملفات (B2B)", 3, "دمشق", "فرع المال والأعمال", "0939111001"),
            ("شركة الاتصالات الموحدة — ديمو أسطول", 2, "حلب", "مقر الإدارة", "0939222002"),
        };

        foreach (var party in multiProfileParties)
        {
            var c = CorporateCustomer.Create(
                party.Name,
                _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST"),
                $"CR-{rnd.Next(100000, 999999)}",
                new PostalAddress(party.Street, party.City, "سوريا", $"{2000 + rnd.Next(7000)}", "سوريا"),
                $"multi-{Guid.NewGuid():N}@syriatel-demo.local",
                party.Phone,
                groups[rnd.Next(groups.Count)],
                categories[rnd.Next(categories.Count)]);
            await _customerRepository.CreateAsync(c);
        }

        var demoDealer = CorporateCustomer.Create(
            "موزع معتمد — ديمو",
            "DLR-001",
            "CR-DEALER-DEMO",
            new PostalAddress("فرع الموزعين", "دمشق", "سوريا", "11111", "سوريا"),
            "dealer-dlr-001@syriatel-demo.local",
            "0939000100",
            groups[rnd.Next(groups.Count)],
            categories[rnd.Next(categories.Count)]);
        await _customerRepository.CreateAsync(demoDealer);

        for (var i = 0; i < 20; i++)
        {
            var city = DemoSyrianSubscriberCatalog.Cities[i % DemoSyrianSubscriberCatalog.Cities.Length];
            var street = DemoSyrianSubscriberCatalog.Streets[i % DemoSyrianSubscriberCatalog.Streets.Length];
            var displayName = DemoSyrianSubscriberCatalog.GetName(i);
            var slug = displayName.Replace(" ", ".", StringComparison.Ordinal).ToLowerInvariant();
            var c = IndividualCustomer.Create(
                displayName,
                _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST"),
                $"010{rnd.Next(1000000, 9999999)}",
                new PostalAddress(street, city, city, $"{10000 + i}", "سوريا"),
                $"{slug}@syriatel-demo.local",
                $"093{rnd.Next(1000000, 9999999)}",
                groups[rnd.Next(groups.Count)],
                categories[rnd.Next(categories.Count)],
                dateOfBirth: new DateOnly(1978 + (i % 20), 1 + (i % 12), 1 + (i % 28)),
                nationality: "سورية",
                gender: i % 2 == 0 ? Gender.Male : Gender.Female,
                occupation: DemoSyrianSubscriberCatalog.Occupations[i % DemoSyrianSubscriberCatalog.Occupations.Length],
                description: $"مشترك نشط — {city} — بيانات ديمو كاملة.");
            await CreateIndividualAsync(c);
        }

        await _unitOfWork.SaveAsync();

        var allCustomers = await _query.Customer.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync();

        SubscriberProfile? heroProfile = null;
        string? heroMsisdnAssetId = null;
        TelecomSubscription? heroSubscription = null;
        SubscriberProfile? demoTakeoverNewOwnerProfile = null;

        var mixOffering = await _query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == "MIX_500")
            .Select(o => new { o.Id, o.ProductId, o.Name })
            .FirstOrDefaultAsync();

        var yaHalaOffering = await _query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == "YA_HALA_30")
            .Select(o => new { o.Id, o.ProductId })
            .FirstOrDefaultAsync();

        var multiProfileCountByPartyName = multiProfileParties.ToDictionary(
            p => p.Name,
            p => p.ProfileCount,
            StringComparer.Ordinal);

        foreach (var cust in allCustomers)
        {
            var isHero = cust.DisplayName.Contains("سعدون الشامي", StringComparison.Ordinal);
            var profileCount = 1;
            if (multiProfileCountByPartyName.TryGetValue(cust.DisplayName, out var mpc))
            {
                profileCount = mpc;
            }

            for (var profileIndex = 0; profileIndex < profileCount; profileIndex++)
            {
                string msisdn;
                if (profileIndex == 0)
                {
                    msisdn = ResolvePrimaryMsisdn(cust, rnd);
                    var clash = await _query.MsisdnAsset.AsNoTracking().AnyAsync(m => !m.IsDeleted && m.Msisdn == msisdn);
                    if (clash && !IsWellKnownDemoMsisdn(msisdn))
                    {
                        msisdn = await GenerateUniqueDemoMsisdnAsync(rnd);
                    }
                }
                else
                {
                    msisdn = await GenerateUniqueDemoMsisdnAsync(rnd);
                }

                var subscriptionTypeId = isHero
                    ? TelecomSubscriptionTypeWellKnownIds.Hybrid
                    : msisdn == TelecomDemoMsisdn.DebtSubscriber
                        ? TelecomSubscriptionTypeWellKnownIds.Postpaid
                        : profileCount > 1
                            ? profileIndex switch
                            {
                                0 => TelecomSubscriptionTypeWellKnownIds.Postpaid,
                                1 => TelecomSubscriptionTypeWellKnownIds.Prepaid,
                                _ => TelecomSubscriptionTypeWellKnownIds.Hybrid
                            }
                            : rnd.Next(3) switch
                            {
                                0 => TelecomSubscriptionTypeWellKnownIds.Prepaid,
                                1 => TelecomSubscriptionTypeWellKnownIds.Postpaid,
                                _ => TelecomSubscriptionTypeWellKnownIds.Hybrid
                            };

                var isPrepaid = subscriptionTypeId == TelecomSubscriptionTypeWellKnownIds.Prepaid;
                var isPostpaid = subscriptionTypeId == TelecomSubscriptionTypeWellKnownIds.Postpaid;
                var isHybrid = subscriptionTypeId == TelecomSubscriptionTypeWellKnownIds.Hybrid;

                // GLOBAL HARDENING: Introduce some suspended lines in bulk for diversity
                var operationalStatus = SubscriberOperationalStatus.Active;
                if (!isHero && profileCount == 1 && rnd.Next(10) == 0)
                {
                    operationalStatus = SubscriberOperationalStatus.Suspended;
                }

                // GLOBAL HLR MOCK DETERMINISTIC ALIGNMENT: Adjust MSISDN last digit based on status
                msisdn = AdjustMsisdnForHlrAlignment(msisdn, operationalStatus, rnd);

                var profile = new SubscriberProfile
                {
                    CustomerId = cust.Id,
                    ServiceLineType = ServiceLineType.Mobile,
                    OperationalStatus = operationalStatus,
                    ActivationDateUtc = DateTime.UtcNow.AddDays(-rnd.Next(30, 800)),
                    LoyaltyPoints = rnd.Next(1000, 25000),
                    LoyaltyTier = rnd.Next(3) == 0 ? "Platinum" : rnd.Next(2) == 0 ? "Gold" : "Silver",
                    ChurnRiskScore = rnd.Next(5, 45)
                };

                if (operationalStatus is SubscriberOperationalStatus.Suspended or SubscriberOperationalStatus.SuspendedInbound or SubscriberOperationalStatus.SuspendedOutbound)
                {
                    if (isPostpaid)
                    {
                        profile.PrepaidBalance = null;
                        profile.PostpaidCreditLimit = -15000m; // GLOBAL HARDENING: Postpaid debt
                    }
                    else
                    {
                        profile.PrepaidBalance = 1500; // GLOBAL HARDENING: Prepaid positive balance
                        profile.PostpaidCreditLimit = null;
                    }
                }
                else
                {
                    profile.PrepaidBalance = (isPrepaid || isHybrid) ? rnd.Next(12500, 85000) : null;
                    profile.PostpaidCreditLimit = (isPostpaid || isHybrid) ? rnd.Next(50000, 300000) : null;
                }

                if (isHero)
                {
                    profile.OperationalStatus = SubscriberOperationalStatus.Active;
                    profile.LoyaltyPoints = 45000;
                    profile.LoyaltyTier = "Platinum";
                    profile.ChurnRiskScore = 8;
                    profile.PrepaidBalance = 24500;
                    profile.PostpaidCreditLimit = 200000;
                }
                else if (profileCount > 1)
                {
                    profile.LoyaltyPoints = 5000 + rnd.Next(1000, 12000) * (profileIndex + 1);
                    profile.LoyaltyTier = profileIndex == 0 ? "Platinum" : "Gold";
                    profile.ChurnRiskScore = 10 + rnd.Next(0, 30);
                }

                await _profileRepository.CreateAsync(profile);
                await _unitOfWork.SaveAsync();

                var kitIccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(msisdn);
                var kitImsi = !string.IsNullOrWhiteSpace(kitIccid)
                    ? MsisdnAssetKitResolver.DeriveImsiFromMsisdn(msisdn)
                    : null;

                var assetProductId = isHero && mixOffering?.ProductId != null
                    ? mixOffering.ProductId
                    : poolCatalog.PickProductForType(subscriptionTypeId, rnd);

                var asset = await _msisdnRepository.GetQuery()
                    .FirstOrDefaultAsync(m => !m.IsDeleted && m.Msisdn == msisdn);
                if (asset == null)
                {
                    asset = new MsisdnAsset
                    {
                        Msisdn = msisdn,
                        PoolStatus = operationalStatus == SubscriberOperationalStatus.Active ? MsisdnPoolStatus.Active : MsisdnPoolStatus.Suspended,
                        SubscriberProfileId = profile.Id,
                        IntendedSubscriptionTypeId = subscriptionTypeId,
                        ProductId = assetProductId,
                    };
                    await _msisdnRepository.CreateAsync(asset);
                }
                else
                {
                    asset.SubscriberProfileId = profile.Id;
                    asset.IntendedSubscriptionTypeId = subscriptionTypeId;
                    asset.ProductId = assetProductId;
                    var targetPoolStatus = operationalStatus == SubscriberOperationalStatus.Active ? MsisdnPoolStatus.Active : MsisdnPoolStatus.Suspended;
                    if (asset.PoolStatus != targetPoolStatus)
                    {
                        asset.TransitionTo(targetPoolStatus);
                    }
                }

                if (demoTakeoverNewOwnerProfile == null
                    && profileIndex == 0
                    && string.Equals(cust.DisplayName, DemoSyrianSubscriberCatalog.GetName(0), StringComparison.Ordinal))
                {
                    demoTakeoverNewOwnerProfile = profile;
                }

                var primaryForParty = profileIndex == 0;
                var sub = new TelecomSubscription
                {
                    SubscriberProfileId = profile.Id,
                    MsisdnAssetId = asset.Id,
                    ProductId = asset.ProductId,
                    ProductOfferingId = isHero && profileIndex == 0 ? mixOffering?.Id : null,
                    SubscriptionTypeId = subscriptionTypeId,
                    DocumentStatus = TelecomDocumentStatus.Verified,
                    IsPrimaryLine = primaryForParty
                };
                await _subscriptionRepository.CreateAsync(sub);

                if (isHero && profileIndex == 0)
                {
                    heroProfile = profile;
                    heroMsisdnAssetId = asset.Id;
                    heroSubscription = sub;
                }

                if (!string.IsNullOrWhiteSpace(kitIccid))
                {
                    var sim = await _simRepository.GetQuery()
                        .FirstOrDefaultAsync(s => !s.IsDeleted && s.Iccid == kitIccid);
                    if (sim == null)
                    {
                        sim = SimInventory.Create(kitIccid, imsi: kitImsi);
                        await _simRepository.CreateAsync(sim);
                    }

                    sim.AssignToProfile(profile.Id);
                    if (sim.Status != SimStatus.Active)
                    {
                        sim.TransitionTo(SimStatus.Active);
                    }

                    _simRepository.Update(sim);
                }

                await _unitOfWork.SaveAsync();
            }
        }

        if (heroProfile != null && !string.IsNullOrEmpty(heroMsisdnAssetId))
        {
            var demoNow = DateTime.UtcNow;
            var (entityName, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.Migration);
            var op = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.Migration,
                Number = _numberSequenceService.GenerateNumber(entityName, prefix, "", useDate: false),
                Status = TelecomOperationStatus.Completed,
                DocumentStatus = TelecomDocumentStatus.Verified,
                SubscriberProfileId = heroProfile.Id,
                MsisdnAssetId = heroMsisdnAssetId,
                ProductOfferingId = mixOffering?.Id,
                ProductId = mixOffering?.ProductId ?? heroSubscription?.ProductId,
                PriorProductOfferingId = yaHalaOffering?.Id,
                PriorProductId = yaHalaOffering?.ProductId,
                TargetOfferName = mixOffering?.Name ?? "سيريتل ميكس 500",
                ConfirmedAtUtc = demoNow.AddMinutes(8),
                CreatedAtUtc = demoNow.AddMinutes(-12),
                Notes = "ديمو MGR مكتمل: يا هلا → سيريتل ميكس 500 — KPIs وعرض 360."
            };
            await _operationRepository.CreateAsync(op);
            await _unitOfWork.SaveAsync();

            await _logRepository.CreateAsync(new BillingIntegrationLog
            {
                TelecomOperationRequestId = op.Id,
                AttemptNumber = 1,
                Success = true,
                Message = "CBS-OK-200: ChangePrimaryOffer MIX_500 applied.",
                IntegrationTarget = "Huawei CBS API v2.1",
            });
            await _unitOfWork.SaveAsync();

            var (vasEntity, vasPrefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.ServiceModification);
            var vasOp = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.ServiceModification,
                Number = _numberSequenceService.GenerateNumber(vasEntity, vasPrefix, "", useDate: false),
                Status = TelecomOperationStatus.Completed,
                DocumentStatus = TelecomDocumentStatus.Verified,
                SubscriberProfileId = heroProfile.Id,
                MsisdnAssetId = heroMsisdnAssetId,
                CreatedAtUtc = demoNow.AddMinutes(-5),
                Notes = "Activate VAS VAS_CALLER_ID — ديمو كاشف الأرقام.",
            };
            await _operationRepository.CreateAsync(vasOp);
            await _unitOfWork.SaveAsync();
        }

        if (heroProfile != null
            && demoTakeoverNewOwnerProfile != null
            && !string.IsNullOrEmpty(heroMsisdnAssetId))
        {
            var (tkoEntity, tkoPrefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.TakeOver);
            var tko = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.TakeOver,
                Number = _numberSequenceService.GenerateNumber(tkoEntity, tkoPrefix, "", useDate: false),
                CorrelationId = Guid.CreateVersion7().ToString(),
                Status = TelecomOperationStatus.PendingDocuments,
                DocumentStatus = TelecomDocumentStatus.Uploaded,
                SubscriberProfileId = heroProfile.Id,
                SecondarySubscriberProfileId = demoTakeoverNewOwnerProfile.Id,
                MsisdnAssetId = heroMsisdnAssetId,
                Notes = "ديمو TKO: نقل ملكية خط سعدون الشامي → أحمد الخطيب — بانتظار اعتماد الباك أوفيس (CBS + HLR)."
            };
            await _operationRepository.CreateAsync(tko);
            await _unitOfWork.SaveAsync();
        }
    }

    /// <summary>
    /// Idempotent §9 demo: fraud-suspended secondary hero line + billing-suspended debt line with completed SUS.
    /// </summary>
    public async Task EnsureHeroReconnectDemoAsync()
    {
        await EnsureSuspendedReconnectDemoLineAsync(
            TelecomDemoMsisdn.ReconnectFraudDemo,
            SuspensionWellKnown.Fraud,
            "ديمو RCN: خط موقوف احتيال — يتطلب اعتماد باك أوفيس.");

        await EnsureSuspendedReconnectDemoLineAsync(
            TelecomDemoMsisdn.DebtSubscriber,
            SuspensionWellKnown.Billing,
            "ديمو RCN: خط موقوف فواتير — مسار تسوية Payment بنقطة البيع.");

        await EnsureSuspendedReconnectDemoLineAsync(
            TelecomDemoMsisdn.ShowcaseOperationalSuspended,
            SuspensionWellKnown.Operational,
            "ديمو RCN: خط موقوف تشغيلي — مسار Operational بدون دفع (VAL-09-04).");
    }

    /// <summary>
    /// Idempotent tribulation showcase: extra active lines on hero customer (healthy + NOT_PROVISIONED HLR).
    /// </summary>
    public async Task EnsureTribulationShowcaseDemoAsync()
    {
        await EnsureActiveTribulationLineAsync(TelecomDemoMsisdn.ShowcaseHealthy, isPrimaryLine: false);
        await EnsureActiveTribulationLineAsync(TelecomDemoMsisdn.ShowcaseNotProvisioned, isPrimaryLine: false);
    }

    private async Task EnsureSuspendedReconnectDemoLineAsync(
        string msisdn,
        string suspensionType,
        string susNotes)
    {
        var customerId = await ResolveShowcaseCustomerIdAsync();
        if (customerId == null)
        {
            return;
        }

        var assetRow = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Msisdn == msisdn)
            .Select(m => new { m.Id, m.SubscriberProfileId, m.PoolStatus })
            .FirstOrDefaultAsync();

        var (profileId, assetId) = await EnsureReconnectDemoLineAsync(
            msisdn,
            customerId,
            assetRow?.Id,
            assetRow?.SubscriberProfileId);

        if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(assetId))
        {
            return;
        }

        profileId = await EnsureDemoLineOwnedByCustomerAsync(profileId, assetId, customerId);
        await ApplyDemoReconnectSuspendedStateAsync(profileId, assetId);

        var hasCompletedSus = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(o => o.Kind == TelecomOperationKind.TemporarySuspension
                           && o.MsisdnAssetId == assetId
                           && o.Status == TelecomOperationStatus.Completed
                           && o.SuspensionType == suspensionType);

        if (hasCompletedSus)
        {
            await ApplyDemoReconnectSuspendedStateAsync(profileId, assetId);
            return;
        }

        var demoNow = DateTime.UtcNow;
        var susDate = msisdn == TelecomDemoMsisdn.DebtSubscriber
            ? demoNow.AddMonths(-7) // Exceeds default 6-month BDR threshold
            : demoNow.AddDays(-14);

        var (entityName, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.TemporarySuspension);
        var sus = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.TemporarySuspension,
            Number = _numberSequenceService.GenerateNumber(entityName, prefix, "", useDate: false),
            Status = TelecomOperationStatus.Completed,
            DocumentStatus = TelecomDocumentStatus.Verified,
            SubscriberProfileId = profileId,
            MsisdnAssetId = assetId,
            SuspensionType = suspensionType,
            SuspensionReason = susNotes,
            BarringLevel = SuspensionWellKnown.BarringFull,
            SuspensionStartDateUtc = susDate,
            BarStatus = "Active",
            ConfirmedAtUtc = susDate,
            CreatedAtUtc = susDate.AddDays(-1),
            Notes = susNotes,
            IsLostOrStolenReport = false,
            FraudClearanceConfirmed = false,
            AutoReconnectEnabled = false,
            NotificationSuppressed = false,
        };

        if (SuspensionWellKnown.IsBackOfficeType(suspensionType))
        {
            sus.ApprovalLevelRequired = "BackOffice";
        }

        await _operationRepository.CreateAsync(sus);
        await _unitOfWork.SaveAsync();
    }

  /// <summary>
  /// Ensures a subscriber profile is linked to a demo MSISDN (pool rows from Oracle may lack <see cref="MsisdnAsset.SubscriberProfileId"/>).
  /// </summary>
    private async Task<(string? ProfileId, string? AssetId)> EnsureReconnectDemoLineAsync(
        string msisdn,
        string customerId,
        string? existingAssetId,
        string? existingProfileId)
    {
        var rnd = new Random();
        if (!string.IsNullOrEmpty(existingAssetId) && !string.IsNullOrEmpty(existingProfileId))
        {
            await EnsureDemoLineKitPairedAsync(existingAssetId, msisdn);
            return (existingProfileId, existingAssetId);
        }

        var productId = await _query.Product.AsNoTracking().IsDeletedEqualTo()
            .Select(p => p.Id)
            .FirstOrDefaultAsync();

        string profileId;
        if (!string.IsNullOrEmpty(existingProfileId))
        {
            profileId = existingProfileId;
            var tracked = await _profileRepository.GetAsync(profileId, CancellationToken.None);
            if (tracked != null)
            {
                if (msisdn == TelecomDemoMsisdn.DebtSubscriber)
                {
                    tracked.PrepaidBalance = null;
                    tracked.PostpaidCreditLimit = -15000m;
                }
                else if (msisdn == TelecomDemoMsisdn.ReconnectFraudDemo)
                {
                    tracked.PrepaidBalance = 0;
                    tracked.PostpaidCreditLimit = null;
                }
                else
                {
                    tracked.PrepaidBalance = 1500;
                    tracked.PostpaidCreditLimit = null;
                }
                _profileRepository.Update(tracked);
                await _unitOfWork.SaveAsync();
            }
        }
        else
        {
            var profile = new SubscriberProfile
            {
                CustomerId = customerId,
                ServiceLineType = ServiceLineType.Mobile,
                OperationalStatus = SubscriberOperationalStatus.Suspended,
                ActivationDateUtc = DateTime.UtcNow.AddDays(-400),
                // GLOBAL LINE TYPE & COMMERCIAL INTEGRITY: Deterministic layout based on MSISDN
                PrepaidBalance = (msisdn == TelecomDemoMsisdn.DebtSubscriber || msisdn == TelecomDemoMsisdn.ReconnectFraudDemo) ? 0 : 1500,
                PostpaidCreditLimit = msisdn == TelecomDemoMsisdn.DebtSubscriber ? -15000m : null
            };
            profile.Suspend(msisdn);
            await _profileRepository.CreateAsync(profile);
            await _unitOfWork.SaveAsync();
            profileId = profile.Id;
        }

        // GLOBAL HLR MOCK DETERMINISTIC ALIGNMENT: Adjust MSISDN for suspended state
        msisdn = AdjustMsisdnForHlrAlignment(msisdn, SubscriberOperationalStatus.Suspended, rnd);

        string assetId;
        if (!string.IsNullOrEmpty(existingAssetId))
        {
            assetId = existingAssetId;
            var tracked = await _msisdnRepository.GetAsync(assetId, CancellationToken.None);
            if (tracked != null)
            {
                tracked.SubscriberProfileId = profileId;
                if (msisdn == TelecomDemoMsisdn.DebtSubscriber)
                {
                    tracked.IntendedSubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Postpaid;
                }
                if (tracked.PoolStatus == MsisdnPoolStatus.Available
                    || tracked.PoolStatus == MsisdnPoolStatus.Reserved)
                {
                    tracked.TransitionTo(MsisdnPoolStatus.Active);
                }

                if (tracked.PoolStatus == MsisdnPoolStatus.Active)
                {
                    tracked.TransitionTo(MsisdnPoolStatus.Suspended);
                }

                if (productId != null && tracked.ProductId == null)
                {
                    tracked.ProductId = productId;
                }

                _msisdnRepository.Update(tracked);
                await _unitOfWork.SaveAsync();
            }
        }
        else
        {
            var asset = new MsisdnAsset
            {
                Msisdn = msisdn,
                SubscriberProfileId = profileId,
                ProductId = productId,
                Category = MsisdnCategory.Normal,
                IntendedSubscriptionTypeId = msisdn == TelecomDemoMsisdn.DebtSubscriber ? TelecomSubscriptionTypeWellKnownIds.Postpaid : TelecomSubscriptionTypeWellKnownIds.Prepaid
            };
            asset.TransitionTo(MsisdnPoolStatus.Active);
            asset.TransitionTo(MsisdnPoolStatus.Suspended);
            await _msisdnRepository.CreateAsync(asset);
            await _unitOfWork.SaveAsync();
            assetId = asset.Id;
        }

        var hasSubscription = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(s => s.SubscriberProfileId == profileId && s.MsisdnAssetId == assetId);

        if (!hasSubscription && productId != null)
        {
            await _subscriptionRepository.CreateAsync(new TelecomSubscription
            {
                SubscriberProfileId = profileId,
                MsisdnAssetId = assetId,
                ProductId = productId,
                SubscriptionTypeId = msisdn == TelecomDemoMsisdn.DebtSubscriber ? TelecomSubscriptionTypeWellKnownIds.Postpaid : TelecomSubscriptionTypeWellKnownIds.Prepaid,
                DocumentStatus = TelecomDocumentStatus.Verified,
                IsPrimaryLine = msisdn != TelecomDemoMsisdn.ReconnectFraudDemo,
            });
            await _unitOfWork.SaveAsync();
        }

        await EnsureDemoLineKitPairedAsync(assetId, msisdn);
        return (profileId, assetId);
    }

    private async Task EnsureDemoLineKitPairedAsync(string msisdnAssetId, string msisdn)
    {
        var iccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(msisdn);
        if (string.IsNullOrEmpty(iccid))
        {
            return;
        }

        await PairMsisdnKitAsync(msisdnAssetId, iccid, MsisdnAssetKitResolver.DeriveImsiFromMsisdn(msisdn));
    }

    private async Task ApplyDemoReconnectSuspendedStateAsync(string profileId, string assetId)
    {
        var asset = await _msisdnRepository.GetAsync(assetId, CancellationToken.None);
        var msisdn = asset?.Msisdn ?? "";
        var profile = await _profileRepository.GetAsync(profileId, CancellationToken.None);
        if (profile != null
            && profile.OperationalStatus is not (
                SubscriberOperationalStatus.Suspended
                or SubscriberOperationalStatus.SuspendedInbound
                or SubscriberOperationalStatus.SuspendedOutbound
                or SubscriberOperationalStatus.Terminated))
        {
            profile.Suspend(msisdn);
            _profileRepository.Update(profile);
        }

        if (asset != null && asset.PoolStatus == MsisdnPoolStatus.Active)
        {
            asset.TransitionTo(MsisdnPoolStatus.Suspended);
        }

        await _unitOfWork.SaveAsync();
    }

    private async Task<string?> ResolveShowcaseCustomerIdAsync()
    {
        var fromHero = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Msisdn == TelecomDemoMsisdn.Hero)
            .Join(
                _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo(),
                m => m.SubscriberProfileId,
                p => p.Id,
                (_, p) => p.CustomerId)
            .FirstOrDefaultAsync();

        if (!string.IsNullOrEmpty(fromHero))
        {
            return fromHero;
        }

        return await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .Where(c => c.DisplayName.Contains(TelecomDemoMsisdn.ShowcaseCustomerName))
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<string> EnsureDemoLineOwnedByCustomerAsync(string profileId, string assetId, string customerId)
    {
        var profile = await _profileRepository.GetAsync(profileId, CancellationToken.None);
        if (profile == null || profile.CustomerId == customerId)
        {
            return profileId;
        }

        var newProfile = new SubscriberProfile
        {
            CustomerId = customerId,
            ServiceLineType = ServiceLineType.Mobile,
            OperationalStatus = profile.OperationalStatus,
            ActivationDateUtc = profile.ActivationDateUtc ?? DateTime.UtcNow.AddDays(-365),
        };

        var asset = await _msisdnRepository.GetAsync(assetId, CancellationToken.None);
        var msisdn = asset?.Msisdn ?? "";

        if (profile.OperationalStatus is SubscriberOperationalStatus.Suspended
            or SubscriberOperationalStatus.SuspendedInbound
            or SubscriberOperationalStatus.SuspendedOutbound)
        {
            newProfile.Suspend(msisdn);
        }

        await _profileRepository.CreateAsync(newProfile);
        await _unitOfWork.SaveAsync();

        if (asset != null)
        {
            asset.SubscriberProfileId = newProfile.Id;
        }

        var subscriptions = await _query.TelecomSubscription.IsDeletedEqualTo()
            .Where(s => s.MsisdnAssetId == assetId && s.SubscriberProfileId == profileId)
            .ToListAsync();

        foreach (var sub in subscriptions)
        {
            var tracked = await _subscriptionRepository.GetAsync(sub.Id, CancellationToken.None);
            if (tracked != null)
            {
                tracked.SubscriberProfileId = newProfile.Id;
                _subscriptionRepository.Update(tracked);
            }
        }

        var operations = await _query.TelecomOperationRequest.IsDeletedEqualTo()
            .Where(o => o.MsisdnAssetId == assetId && o.SubscriberProfileId == profileId)
            .ToListAsync();

        foreach (var op in operations)
        {
            var tracked = await _operationRepository.GetAsync(op.Id, CancellationToken.None);
            if (tracked != null)
            {
                tracked.SubscriberProfileId = newProfile.Id;
                _operationRepository.Update(tracked);
            }
        }

        await _unitOfWork.SaveAsync();
        return newProfile.Id;
    }

    private async Task EnsureActiveTribulationLineAsync(string msisdn, bool isPrimaryLine)
    {
        var customerId = await ResolveShowcaseCustomerIdAsync();
        if (customerId == null)
        {
            return;
        }

        var assetRow = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Msisdn == msisdn)
            .Select(m => new { m.Id, m.SubscriberProfileId })
            .FirstOrDefaultAsync();

        var productId = await _query.Product.AsNoTracking().IsDeletedEqualTo()
            .Select(p => p.Id)
            .FirstOrDefaultAsync();

        if (productId == null)
        {
            return;
        }

        string profileId;
        string assetId;

        if (!string.IsNullOrEmpty(assetRow?.Id))
        {
            assetId = assetRow.Id;

            if (!string.IsNullOrEmpty(assetRow.SubscriberProfileId))
            {
                profileId = await EnsureDemoLineOwnedByCustomerAsync(
                    assetRow.SubscriberProfileId,
                    assetId,
                    customerId);
            }
            else
            {
                var profile = new SubscriberProfile
                {
                    CustomerId = customerId,
                    ServiceLineType = ServiceLineType.Mobile,
                    OperationalStatus = SubscriberOperationalStatus.Active,
                    ActivationDateUtc = DateTime.UtcNow.AddDays(-200),
                    // GLOBAL HARDENING: Prepaid active line
                    PrepaidBalance = 5000,
                    PostpaidCreditLimit = null
                };
                await _profileRepository.CreateAsync(profile);
                await _unitOfWork.SaveAsync();
                profileId = profile.Id;
            }

            var trackedProfile = await _profileRepository.GetAsync(profileId, CancellationToken.None);
            if (trackedProfile != null && trackedProfile.OperationalStatus != SubscriberOperationalStatus.Active)
            {
                trackedProfile.Activate();
                _profileRepository.Update(trackedProfile);
            }

            var asset = await _msisdnRepository.GetAsync(assetId, CancellationToken.None);
            if (asset != null)
            {
                asset.SubscriberProfileId = profileId;
                if (asset.PoolStatus is MsisdnPoolStatus.Available or MsisdnPoolStatus.Reserved
                    or MsisdnPoolStatus.Suspended)
                {
                    asset.TransitionTo(MsisdnPoolStatus.Active);
                }

                if (asset.ProductId == null)
                {
                    asset.ProductId = productId;
                }

                _msisdnRepository.Update(asset);
            }

            await _unitOfWork.SaveAsync();
        }
        else
        {
            var profile = new SubscriberProfile
            {
                CustomerId = customerId,
                ServiceLineType = ServiceLineType.Mobile,
                OperationalStatus = SubscriberOperationalStatus.Active,
                ActivationDateUtc = DateTime.UtcNow.AddDays(-200),
                // GLOBAL HARDENING: Prepaid active line
                PrepaidBalance = 5000,
                PostpaidCreditLimit = null
            };
            await _profileRepository.CreateAsync(profile);
            await _unitOfWork.SaveAsync();
            profileId = profile.Id;

            var asset = new MsisdnAsset
            {
                Msisdn = msisdn,
                SubscriberProfileId = profileId,
                ProductId = productId,
                Category = MsisdnCategory.Normal,
            };
            asset.TransitionTo(MsisdnPoolStatus.Active);
            await _msisdnRepository.CreateAsync(asset);
            await _unitOfWork.SaveAsync();
            assetId = asset.Id;
        }

        var hasSubscription = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(s => s.SubscriberProfileId == profileId && s.MsisdnAssetId == assetId);

        if (!hasSubscription)
        {
            await _subscriptionRepository.CreateAsync(new TelecomSubscription
            {
                SubscriberProfileId = profileId,
                MsisdnAssetId = assetId,
                ProductId = productId,
                SubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Prepaid,
                DocumentStatus = TelecomDocumentStatus.Verified,
                IsPrimaryLine = isPrimaryLine,
            });
            await _unitOfWork.SaveAsync();
        }

        await EnsureDemoLineKitPairedAsync(assetId, msisdn);
    }

    /// <summary>
    /// Idempotent §16 demo: pending write-off BDR on debt showcase line (0939000002 / سعدون الشامي).
    /// Runs after <see cref="EnsureHeroReconnectDemoAsync"/> so billing suspension exists.
    /// </summary>
    public async Task EnsureHeroBadDebtDemoAsync()
    {
        var assetRow = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Msisdn == TelecomDemoMsisdn.DebtSubscriber)
            .Select(m => new { m.Id, m.SubscriberProfileId })
            .FirstOrDefaultAsync();

        var debtCustomerId = await ResolveShowcaseCustomerIdAsync();

        if (debtCustomerId == null)
        {
            return;
        }

        var (profileId, assetId) = await EnsureReconnectDemoLineAsync(
            TelecomDemoMsisdn.DebtSubscriber,
            debtCustomerId,
            assetRow?.Id,
            assetRow?.SubscriberProfileId);

        if (!string.IsNullOrEmpty(profileId) && !string.IsNullOrEmpty(assetId))
        {
            profileId = await EnsureDemoLineOwnedByCustomerAsync(profileId, assetId, debtCustomerId);
        }

        if (string.IsNullOrEmpty(profileId) || string.IsNullOrEmpty(assetId))
        {
            return;
        }

        // GLOBAL HARDENING: Force update any existing BDR for this line to ensure it shows up for Saadoon
        var existingBdr = await _query.TelecomOperationRequest.IsDeletedEqualTo()
            .FirstOrDefaultAsync(o => o.Kind == TelecomOperationKind.BadDebtRecovery
                           && o.MsisdnAssetId == assetId);

        if (existingBdr != null)
        {
            var trackedBdr = await _operationRepository.GetAsync(existingBdr.Id, CancellationToken.None);
            if (trackedBdr != null)
            {
                trackedBdr.SubscriberProfileId = profileId;
                trackedBdr.Status = TelecomOperationStatus.PendingDocuments;
                trackedBdr.ApprovalLevelRequired = "BackOffice";
                trackedBdr.DocumentStatus = TelecomDocumentStatus.Verified; // User requested "Verified"
                trackedBdr.OutstandingBalanceSnapshot = -15000m;
                trackedBdr.WriteOffAmount = 10000m;
                trackedBdr.CollectedAmount = 5000m; // Required Cash
                trackedBdr.CollectionAction = "WriteOffPartial";
                trackedBdr.DunningStage = "WriteOffPending";
                trackedBdr.Notes = "BDR-0001|demo=WriteOffPartial|balance=-15000|writeoff=10000|cash=5000|bo=true";
                
                // GLOBAL HARDENING: SLA for demo BDR
                var slaMinutes = 2;
                trackedBdr.SlaExpirationTimeUtc = DateTime.UtcNow.AddMinutes(slaMinutes);
                
                _operationRepository.Update(trackedBdr);
                await _unitOfWork.SaveAsync();
            }
        }
        else
        {
            var demoNow = DateTime.UtcNow;
            var bdr = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.BadDebtRecovery,
                Number = "BDR-0001", // Explicitly BDR-0001
                CorrelationId = Guid.CreateVersion7().ToString(),
                Status = TelecomOperationStatus.PendingDocuments,
                IdentityDocumentStorageKey = "demo-bdr-identity-key", // Fixed: Allow BackOffice Approval
                DocumentStatus = TelecomDocumentStatus.Verified,
                SubscriberProfileId = profileId,
                MsisdnAssetId = assetId,
                CollectionAction = "WriteOffPartial",
                DunningStage = "WriteOffPending",
                PriorDunningStage = "Reminder2",
                OutstandingBalanceSnapshot = -15_000m,
                WriteOffAmount = 10_000m,
                CollectedAmount = 5_000m, // Required Cash
                CollectionNote = "ديمو BDR: شطب جزئي معلّق — اعتماد باك أوفيس.",
                CollectionSettlementStatus = "Pending",
                ApprovalLevelRequired = "BackOffice",
                ProvisioningResult = "Pending",
                Notes = "BDR-0001|demo=WriteOffPartial|balance=-15000|writeoff=10000|cash=5000|bo=true",
                IsLostOrStolenReport = false,
                FraudClearanceConfirmed = false,
                AutoReconnectEnabled = false,
                NotificationSuppressed = false,
                CreatedAtUtc = demoNow.AddHours(-2),
                SlaExpirationTimeUtc = DateTime.UtcNow.AddMinutes(2),
            };

            await _operationRepository.CreateAsync(bdr);
            await _unitOfWork.SaveAsync();
        }
    }
}
