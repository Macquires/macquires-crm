using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.Reconnect;
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
    private readonly ICommandRepository<TelecomPaymentTransaction> _paymentRepository;
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFieldEncryptionService _encryption;
    private readonly IBillingSystemIntegration _billing;
    private readonly IHLRLiveStatusService _hlr;

    public TelecomSyriatelSeeder(
        IQueryContext query,
        ICommandRepository<Customer> customerRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<BillingIntegrationLog> logRepository,
        ICommandRepository<TelecomPaymentTransaction> paymentRepository,
        ICommandRepository<TelecomTechnicalTicket> ticketRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork,
        IFieldEncryptionService encryption,
        IBillingSystemIntegration billing,
        IHLRLiveStatusService hlr)
    {
        _query = query;
        _customerRepository = customerRepository;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _subscriptionRepository = subscriptionRepository;
        _simRepository = simRepository;
        _operationRepository = operationRepository;
        _logRepository = logRepository;
        _paymentRepository = paymentRepository;
        _ticketRepository = ticketRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
        _encryption = encryption;
        _billing = billing;
        _hlr = hlr;
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
        DemoSeedScope.StampCustomer(customer);
        customer.SyncNationalIdSearchHash(_encryption);
        await _customerRepository.CreateAsync(customer);
    }

    private async Task<HashSet<string>> LoadReservedNationalIdHashesAsync()
    {
        var individuals = await _query.Customer
            .IgnoreQueryFilters()
            .AsNoTracking()
            .IsDeletedEqualTo()
            .OfType<IndividualCustomer>()
            .Select(c => new { c.NationalId, c.NationalIdSearchHash })
            .ToListAsync();

        var reserved = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in individuals)
        {
            if (!string.IsNullOrWhiteSpace(row.NationalIdSearchHash))
            {
                reserved.Add(row.NationalIdSearchHash);
            }

            if (!string.IsNullOrWhiteSpace(row.NationalId))
            {
                reserved.Add(_encryption.ComputeSearchHash(row.NationalId.Trim()));
            }
        }

        return reserved;
    }

    private string AllocateUniqueDemoNationalId(HashSet<string> reservedHashes, Random rnd)
    {
        for (var attempt = 0; attempt < 80; attempt++)
        {
            var nationalId = $"010{rnd.Next(1000000, 9999999)}";
            if (reservedHashes.Add(_encryption.ComputeSearchHash(nationalId)))
            {
                return nationalId;
            }
        }

        throw new InvalidOperationException("TelecomSyriatelSeeder: could not allocate a unique demo NationalId.");
    }

    private bool TryReserveNationalId(HashSet<string> reservedHashes, string nationalId) =>
        reservedHashes.Add(_encryption.ComputeSearchHash(nationalId));

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
            var migrationNumber = await _numberSequenceService.GenerateNumberAsync(entityName, prefix, "", useDate: false);
            var op = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.Migration,
                Number = migrationNumber,
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
            var vasNumber = await _numberSequenceService.GenerateNumberAsync(vasEntity, vasPrefix, "", useDate: false);
            await _operationRepository.CreateAsync(new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.ServiceModification,
                Number = vasNumber,
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
        {
            return;
        }

        var rnd = new Random(20260512);
        await ApplyDemoInventoryStatePatchesAsync(poolCatalog, rnd);

        var heroDemoReady = await _query.MsisdnAsset.AsNoTracking()
            .AnyAsync(m => !m.IsDeleted
                           && m.Msisdn == TelecomDemoMsisdn.Hero
                           && m.SubscriberProfileId != null);
        if (heroDemoReady)
        {
            return;
        }

        var groups = await _query.CustomerGroup.AsNoTracking().Select(x => x.Id).ToListAsync();
        var categories = await _query.CustomerCategory.AsNoTracking().Select(x => x.Id).ToListAsync();
        var reservedNationalIdHashes = await LoadReservedNationalIdHashesAsync();

        const string heroNationalId = "0109988776";
        const string debtNationalId = "0108877665";

        var heroAccount = await _numberSequenceService.GenerateNumberAsync(nameof(Customer), "", "CST");
        var heroCustomer = IndividualCustomer.Create(
            "سعدون الشامي",
            heroAccount,
            heroNationalId,
            new PostalAddress("مشروع دمر — سكن جديد", "دمشق", "دمشق", "22000", "سوريا"),
            "sadoun.alshami@syriatel-demo.local",
            TelecomDemoMsisdn.Hero,
            groups[rnd.Next(groups.Count)],
            categories[rnd.Next(categories.Count)],
            description: "عميل منذ نحو 10 سنوات — سيناريو ديمو.");
        if (TryReserveNationalId(reservedNationalIdHashes, heroNationalId))
        {
            await CreateIndividualAsync(heroCustomer);
        }

        var debtAccount = await _numberSequenceService.GenerateNumberAsync(nameof(Customer), "", "CST");
        var debtCustomer = IndividualCustomer.Create(
            "مازن المديون",
            debtAccount,
            debtNationalId,
            new PostalAddress("دمشق القديمة", "دمشق", "دمشق", "00000", "سوريا"),
            "mazen.demo@syriatel-demo.local",
            TelecomDemoMsisdn.DebtSubscriber,
            groups[rnd.Next(groups.Count)],
            categories[rnd.Next(categories.Count)]);
        debtCustomer.SetDescription("مشترك مديون — خط موقوف Billing منذ أكثر من 6 أشهر (−15,000 ل.س) — سيناريو الدفع بالمعرض.");
        if (TryReserveNationalId(reservedNationalIdHashes, debtNationalId))
        {
            await CreateIndividualAsync(debtCustomer);
        }

        var multiProfileParties = new List<(string Name, int ProfileCount, string City, string Street, string Phone)>
        {
            ("البنك العربي — ديمو تعدد ملفات (B2B)", 3, "دمشق", "فرع المال والأعمال", "0939111001"),
            ("شركة الاتصالات الموحدة — ديمو أسطول", 2, "حلب", "مقر الإدارة", "0939222002"),
        };

        foreach (var party in multiProfileParties)
        {
            var corporateAccount = await _numberSequenceService.GenerateNumberAsync(nameof(Customer), "", "CST");
            var c = CorporateCustomer.Create(
                party.Name,
                corporateAccount,
                $"CR-{rnd.Next(100000, 999999)}",
                new PostalAddress(party.Street, party.City, "سوريا", $"{2000 + rnd.Next(7000)}", "سوريا"),
                $"multi-{Guid.NewGuid():N}@syriatel-demo.local",
                party.Phone,
                groups[rnd.Next(groups.Count)],
                categories[rnd.Next(categories.Count)]);
            DemoSeedScope.StampCustomer(c);
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
        DemoSeedScope.StampCustomer(demoDealer);
        await _customerRepository.CreateAsync(demoDealer);

        for (var i = 0; i < 20; i++)
        {
            var city = DemoSyrianSubscriberCatalog.Cities[i % DemoSyrianSubscriberCatalog.Cities.Length];
            var street = DemoSyrianSubscriberCatalog.Streets[i % DemoSyrianSubscriberCatalog.Streets.Length];
            var displayName = DemoSyrianSubscriberCatalog.GetName(i);
            var slug = displayName.Replace(" ", ".", StringComparison.Ordinal).ToLowerInvariant();
            var individualAccount = await _numberSequenceService.GenerateNumberAsync(nameof(Customer), "", "CST");
            var c = IndividualCustomer.Create(
                displayName,
                individualAccount,
                AllocateUniqueDemoNationalId(reservedNationalIdHashes, rnd),
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

        var debtPostpaidOffering = await _query.ProductOffering.AsNoTracking()
            .Where(o => !o.IsDeleted && o.Code == TelecomDemoBaselines.DebtPostpaidOfferingCode)
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

                var isDebtShowcase = msisdn == TelecomDemoMsisdn.DebtSubscriber;

                // GLOBAL HARDENING: Introduce some suspended lines in bulk for diversity
                var operationalStatus = SubscriberOperationalStatus.Active;
                if (isDebtShowcase)
                {
                    operationalStatus = SubscriberOperationalStatus.Suspended;
                }
                else if (!isHero && profileCount == 1 && rnd.Next(10) == 0)
                {
                    operationalStatus = SubscriberOperationalStatus.Suspended;
                }

                // GLOBAL HLR MOCK DETERMINISTIC ALIGNMENT: Adjust MSISDN last digit based on status
                // EXCEPTION: Debt subscriber (0939000002) keeps original digit (2 = ACTIVE) for Revenue Leakage demo
                // where CRM is Suspended but HLR is ACTIVE - creating the mismatch detection scenario
                if (!isDebtShowcase)
                {
                    msisdn = AdjustMsisdnForHlrAlignment(msisdn, operationalStatus, rnd);
                }

                var profile = new SubscriberProfile
                {
                    CustomerId = cust.Id,
                    BranchId = cust.BranchId,
                    ServiceLineType = ServiceLineType.Mobile,
                    OperationalStatus = operationalStatus,
                    ActivationDateUtc = DateTime.UtcNow.AddDays(-rnd.Next(30, 800)),
                    LoyaltyPoints = rnd.Next(1000, 25000),
                    LoyaltyTier = rnd.Next(3) == 0 ? "Platinum" : rnd.Next(2) == 0 ? "Gold" : "Silver",
                    ChurnRiskScore = rnd.Next(5, 45)
                };

                if (operationalStatus is SubscriberOperationalStatus.Suspended or SubscriberOperationalStatus.SuspendedInbound or SubscriberOperationalStatus.SuspendedOutbound)
                {
                    if (isPostpaid || isDebtShowcase)
                    {
                        profile.PrepaidBalance = null;
                        profile.PostpaidCreditLimit = TelecomDemoBaselines.DebtOutstandingSyp;
                    }
                    else
                    {
                        profile.PrepaidBalance = 1500;
                        profile.PostpaidCreditLimit = null;
                    }
                }
                else
                {
                    profile.PrepaidBalance = (isPrepaid || isHybrid) ? rnd.Next(12500, 85000) : null;
                    profile.PostpaidCreditLimit = (isPostpaid || isHybrid) ? rnd.Next(50000, 300000) : null;
                }

                if (isDebtShowcase)
                {
                    profile.PrepaidBalance = null;
                    profile.PostpaidCreditLimit = TelecomDemoBaselines.DebtOutstandingSyp;
                    profile.ActivationDateUtc = TelecomDemoBaselines.DebtLineActivatedUtc();
                    profile.ChurnRiskScore = 68;
                    profile.LoyaltyTier = "Silver";
                    profile.LoyaltyPoints = 1800;
                }
                else if (isHero)
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
                    : isDebtShowcase && debtPostpaidOffering?.ProductId != null
                        ? debtPostpaidOffering.ProductId
                        : poolCatalog.PickProductForType(subscriptionTypeId, rnd);

                var asset = await _msisdnRepository.GetQuery()
                    .FirstOrDefaultAsync(m => !m.IsDeleted && m.Msisdn == msisdn);
                if (asset == null)
                {
                    asset = new MsisdnAsset
                    {
                        Msisdn = msisdn,
                        BranchId = cust.BranchId,
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
                    asset.BranchId = cust.BranchId ?? asset.BranchId;
                    asset.IntendedSubscriptionTypeId = subscriptionTypeId;
                    asset.ProductId = assetProductId;
                var targetPoolStatus = operationalStatus == SubscriberOperationalStatus.Active ? MsisdnPoolStatus.Active : MsisdnPoolStatus.Suspended;
                if (asset.PoolStatus != targetPoolStatus)
                {
                    // Hardening: Available/Reserved must pass through Active before Suspended (state machine enforcement)
                    if (targetPoolStatus == MsisdnPoolStatus.Suspended
                        && asset.PoolStatus is MsisdnPoolStatus.Available or MsisdnPoolStatus.Reserved)
                    {
                        asset.TransitionTo(MsisdnPoolStatus.Active);
                    }
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
                    ProductOfferingId = isHero && profileIndex == 0
                        ? mixOffering?.Id
                        : isDebtShowcase
                            ? debtPostpaidOffering?.Id
                            : null,
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
                        DemoSeedScope.ApplySimScope(sim, cust.BranchId);
                        await _simRepository.CreateAsync(sim);
                    }
                    else
                    {
                        DemoSeedScope.ApplySimScope(sim, cust.BranchId);
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
            var heroMigrationNumber = await _numberSequenceService.GenerateNumberAsync(entityName, prefix, "", useDate: false);
            var op = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.Migration,
                Number = heroMigrationNumber,
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
            DemoSeedScope.ApplyOperationScope(op, null);
            await _operationRepository.CreateAsync(op);
            await _unitOfWork.SaveAsync();

            await _logRepository.CreateAsync(new BillingIntegrationLog
            {
                TelecomOperationRequestId = op.Id,
                AttemptNumber = 1,
                Success = true,
                Message = "CBS-OK-200: ChangePrimaryOffer MIX_500 applied.",
                IntegrationTarget = "Huawei CBS API v2.1",
                BranchId = op.BranchId,
            });
            await _unitOfWork.SaveAsync();

            var (vasEntity, vasPrefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.ServiceModification);
            var heroVasNumber = await _numberSequenceService.GenerateNumberAsync(vasEntity, vasPrefix, "", useDate: false);
            var vasOp = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.ServiceModification,
                Number = heroVasNumber,
                Status = TelecomOperationStatus.Completed,
                DocumentStatus = TelecomDocumentStatus.Verified,
                SubscriberProfileId = heroProfile.Id,
                MsisdnAssetId = heroMsisdnAssetId,
                CreatedAtUtc = demoNow.AddMinutes(-5),
                Notes = "Activate VAS VAS_CALLER_ID — ديمو كاشف الأرقام.",
            };
            DemoSeedScope.ApplyOperationScope(vasOp, null);
            await _operationRepository.CreateAsync(vasOp);
            await _unitOfWork.SaveAsync();
        }

        if (heroProfile != null
            && demoTakeoverNewOwnerProfile != null
            && !string.IsNullOrEmpty(heroMsisdnAssetId))
        {
            var (tkoEntity, tkoPrefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.TakeOver);
            var tkoNumber = await _numberSequenceService.GenerateNumberAsync(tkoEntity, tkoPrefix, "", useDate: false);
            var tko = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.TakeOver,
                Number = tkoNumber,
                CorrelationId = Guid.CreateVersion7().ToString(),
                Status = TelecomOperationStatus.PendingDocuments,
                DocumentStatus = TelecomDocumentStatus.Uploaded,
                IdentityDocumentStorageKey = "demo-tko-identity-key",
                ApprovalLevelRequired = "BackOffice",
                SubscriberProfileId = heroProfile.Id,
                SecondarySubscriberProfileId = demoTakeoverNewOwnerProfile.Id,
                MsisdnAssetId = heroMsisdnAssetId,
                Notes = "ديمو TKO: نقل ملكية خط سعدون الشامي → أحمد الخطيب — بانتظار اعتماد الباك أوفيس (CBS + HLR)."
            };
            DemoSeedScope.ApplyOperationScope(tko, null);
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

        await EnsurePendingReconnectDemoOperationsAsync();
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
        var customerId = msisdn == TelecomDemoMsisdn.DebtSubscriber
            ? await ResolveDebtShowcaseCustomerIdAsync() ?? await ResolveShowcaseCustomerIdAsync()
            : await ResolveShowcaseCustomerIdAsync();
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
            ? TelecomDemoBaselines.DebtBillingSuspensionStartUtc(demoNow)
            : demoNow.AddDays(-14);

        var (entityName, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.TemporarySuspension);
        var suspensionNumber = await _numberSequenceService.GenerateNumberAsync(entityName, prefix, "", useDate: false);
        var sus = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.TemporarySuspension,
            Number = suspensionNumber,
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

        var branchId = await DemoSeedScope.ResolveCustomerBranchIdAsync(_query, customerId);
        DemoSeedScope.ApplyOperationScope(sus, branchId);

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
        var branchId = await DemoSeedScope.ResolveCustomerBranchIdAsync(_query, customerId);
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
                    tracked.PostpaidCreditLimit = TelecomDemoBaselines.DebtOutstandingSyp;
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
                PostpaidCreditLimit = msisdn == TelecomDemoMsisdn.DebtSubscriber ? TelecomDemoBaselines.DebtOutstandingSyp : null
            };
            DemoSeedScope.ApplyProfileScope(profile, branchId);
            profile.Suspend(msisdn);
            await _profileRepository.CreateAsync(profile);
            await _unitOfWork.SaveAsync();
            profileId = profile.Id;
        }

        // GLOBAL HLR MOCK DETERMINISTIC ALIGNMENT: Adjust MSISDN for suspended state
        // EXCEPTION: Debt subscriber (0939000002) keeps original digit (2 = ACTIVE) for Revenue Leakage demo
        // where CRM is Suspended but HLR is ACTIVE - creating the mismatch detection scenario
        if (msisdn != TelecomDemoMsisdn.DebtSubscriber)
        {
            msisdn = AdjustMsisdnForHlrAlignment(msisdn, SubscriberOperationalStatus.Suspended, rnd);
        }

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
                // Hardening: Available/Reserved must pass through Active before Suspended (state machine enforcement)
                if (tracked.PoolStatus is MsisdnPoolStatus.Available or MsisdnPoolStatus.Reserved)
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
            DemoSeedScope.ApplyMsisdnScope(asset, branchId);
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

        if (asset != null)
        {
            // Hardening: Available/Reserved must pass through Active before Suspended (state machine enforcement)
            if (asset.PoolStatus is MsisdnPoolStatus.Available or MsisdnPoolStatus.Reserved)
            {
                asset.TransitionTo(MsisdnPoolStatus.Active);
            }
            if (asset.PoolStatus == MsisdnPoolStatus.Active)
            {
                asset.TransitionTo(MsisdnPoolStatus.Suspended);
            }
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

    private async Task<string?> ResolveDebtShowcaseCustomerIdAsync()
    {
        return await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .Where(c => c.DisplayName.Contains(TelecomDemoMsisdn.DebtShowcaseCustomerName))
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

        var branchId = await DemoSeedScope.ResolveCustomerBranchIdAsync(_query, customerId);

        var newProfile = new SubscriberProfile
        {
            CustomerId = customerId,
            ServiceLineType = ServiceLineType.Mobile,
            OperationalStatus = profile.OperationalStatus,
            ActivationDateUtc = profile.ActivationDateUtc ?? DateTime.UtcNow.AddDays(-365),
        };
        DemoSeedScope.ApplyProfileScope(newProfile, branchId);

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
            DemoSeedScope.ApplyMsisdnScope(asset, branchId);
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

        var branchId = await DemoSeedScope.ResolveCustomerBranchIdAsync(_query, customerId);
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
                DemoSeedScope.ApplyProfileScope(profile, branchId);
                await _profileRepository.CreateAsync(profile);
                await _unitOfWork.SaveAsync();
                profileId = profile.Id;
            }

            var trackedProfile = await _profileRepository.GetAsync(profileId, CancellationToken.None);
            if (trackedProfile != null)
            {
                DemoSeedScope.ApplyProfileScope(trackedProfile, branchId);
                if (trackedProfile.OperationalStatus != SubscriberOperationalStatus.Active)
                {
                    trackedProfile.Activate();
                }

                _profileRepository.Update(trackedProfile);
            }

            var asset = await _msisdnRepository.GetAsync(assetId, CancellationToken.None);
            if (asset != null)
            {
                asset.SubscriberProfileId = profileId;
                DemoSeedScope.ApplyMsisdnScope(asset, branchId);
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
                PrepaidBalance = 5000,
                PostpaidCreditLimit = null
            };
            DemoSeedScope.ApplyProfileScope(profile, branchId);
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
            DemoSeedScope.ApplyMsisdnScope(asset, branchId);
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
    /// Idempotent national-anchor reconciliation — debt line baseline + clean back-office slate.
    /// </summary>
    public async Task EnsureHeroBadDebtDemoAsync()
    {
        var assetRow = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Msisdn == TelecomDemoMsisdn.DebtSubscriber)
            .Select(m => new { m.Id, m.SubscriberProfileId })
            .FirstOrDefaultAsync();

        var debtCustomerId = await ResolveDebtShowcaseCustomerIdAsync()
            ?? await ResolveShowcaseCustomerIdAsync();

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

        await CreateDemoAnchorReconciler().ReconcileAllAsync();
    }

    private DemoAnchorBaselineReconciler CreateDemoAnchorReconciler() =>
        new(
            _query,
            _profileRepository,
            _msisdnRepository,
            _subscriptionRepository,
            _operationRepository,
            _logRepository,
            _paymentRepository,
            _ticketRepository,
            _unitOfWork,
            _billing,
            _hlr);

    /// <summary>
    /// Pending RCN rows for back-office approve demo (Fraud / Operational only).
    /// Debt line (0939000002) starts clean — PAY → RCN created live from the showroom.
    /// </summary>
    private async Task EnsurePendingReconnectDemoOperationsAsync()
    {
        await EnsurePendingReconnectForLineAsync(
            TelecomDemoMsisdn.ReconnectFraudDemo,
            ReconnectWellKnown.Fraud,
            "ديمو RCN-0001: موقوف احتيال — اعتماد باك أوفيس.",
            fraudClearanceConfirmed: true);

        await EnsurePendingReconnectForLineAsync(
            TelecomDemoMsisdn.ShowcaseOperationalSuspended,
            ReconnectWellKnown.Operational,
            "ديمو RCN-0002: موقوف تشغيلي — اعتماد باك أوفيس.");
    }

    private async Task EnsurePendingReconnectForLineAsync(
        string msisdn,
        string clearanceType,
        string notes,
        string? paymentReference = null,
        bool fraudClearanceConfirmed = false,
        bool seedPaymentReceipt = false)
    {
        var assetRow = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Msisdn == msisdn)
            .Select(m => new { m.Id, m.SubscriberProfileId })
            .FirstOrDefaultAsync();
        if (assetRow == null || string.IsNullOrEmpty(assetRow.SubscriberProfileId))
        {
            return;
        }

        var hasPending = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(o => o.Kind == TelecomOperationKind.Reconnect
                           && o.MsisdnAssetId == assetRow.Id
                           && (o.Status == TelecomOperationStatus.PendingDocuments
                               || o.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance));
        if (hasPending)
        {
            return;
        }

        if (seedPaymentReceipt && !string.IsNullOrEmpty(paymentReference))
        {
            await EnsureDemoPaymentReceiptAsync(paymentReference);
        }

        var sourceSuspensionId = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => o.Kind == TelecomOperationKind.TemporarySuspension
                        && o.MsisdnAssetId == assetRow.Id
                        && o.Status == TelecomOperationStatus.Completed)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => o.Id)
            .FirstOrDefaultAsync();

        var status = !string.IsNullOrEmpty(paymentReference)
            ? TelecomOperationStatus.Paid_Pending_BackOffice_Clearance
            : TelecomOperationStatus.PendingDocuments;

        var (entityName, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.Reconnect);
        var rcnNumber = await _numberSequenceService.GenerateNumberAsync(entityName, prefix, "", useDate: false);
        var rcn = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.Reconnect,
            Number = rcnNumber,
            CorrelationId = Guid.CreateVersion7().ToString(),
            Status = status,
            ApprovalLevelRequired = "BackOffice",
            DocumentStatus = TelecomDocumentStatus.Verified,
            IdentityDocumentStorageKey = "demo-rcn-identity-key",
            SubscriberProfileId = assetRow.SubscriberProfileId,
            MsisdnAssetId = assetRow.Id,
            SourceSuspensionOperationId = sourceSuspensionId,
            ClearanceType = clearanceType,
            ReconnectReason = string.Equals(clearanceType, ReconnectWellKnown.Payment, StringComparison.OrdinalIgnoreCase)
                ? "LateBillPayment"
                : "CustomerRequest",
            PaymentReference = paymentReference,
            FraudClearanceConfirmed = fraudClearanceConfirmed,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            Notes = notes,
        };
        DemoSeedScope.ApplyOperationScope(rcn, null);
        await _operationRepository.CreateAsync(rcn);
        await _unitOfWork.SaveAsync();
    }

    private async Task EnsureDemoPaymentReceiptAsync(string receiptNumber)
    {
        var exists = await _query.BillingIntegrationLog.AsNoTracking()
            .AnyAsync(l => !l.IsDeleted && l.Success && l.CorrelationId == receiptNumber);
        if (exists)
        {
            return;
        }

        await _logRepository.CreateAsync(new BillingIntegrationLog
        {
            Success = true,
            CorrelationId = receiptNumber,
            AttemptNumber = 1,
            Message = "CBS-OK-200: Demo bill pay 15000 SYP (RCPT-2002).",
            IntegrationTarget = "Huawei CBS API v2.1",
            BranchId = null,
        });
        await _unitOfWork.SaveAsync();
    }
}
