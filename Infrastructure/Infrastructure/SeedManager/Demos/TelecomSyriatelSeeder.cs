using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
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
        IUnitOfWork unitOfWork)
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

    private static bool IsWellKnownDemoMsisdn(string msisdn) =>
        msisdn == TelecomDemoMsisdn.Hero
        || msisdn == TelecomDemoMsisdn.DebtSubscriber
        || msisdn == TelecomDemoMsisdn.ReconnectFraudDemo;

    private static string ResolvePrimaryMsisdn(Customer cust, Random rnd)
    {
        if (cust.DisplayName.Contains("سعدون الشامي", StringComparison.Ordinal))
        {
            return TelecomDemoMsisdn.Hero;
        }

        if (cust.DisplayName.Contains("مازن المديون", StringComparison.Ordinal))
        {
            return TelecomDemoMsisdn.DebtSubscriber;
        }

        if (!string.IsNullOrWhiteSpace(cust.PrimaryPhone))
        {
            return TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(cust.PrimaryPhone)
                ?? cust.PrimaryPhone.Replace(" ", "").Trim();
        }

        return $"093{Math.Abs(cust.Id.GetHashCode()) % 10_000_000:0000000}";
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
        var productIds = await _query.Product
            .AsNoTracking()
            .Where(p => p.Physical == false && !p.IsDeleted && p.ServiceCode != null)
            .OrderBy(p => p.Name)
            .Select(p => p.Id)
            .ToListAsync();

        if (!productIds.Any())
            return;

        var rnd = new Random(20260512);

        // 1. Available Pool (100 numbers)
        for (int i = 1; i <= 100; i++)
        {
            var pfx = i % 2 == 0 ? "093" : "099";
            var num = $"{pfx}{i + 2000000:0000000}";
            if (!await _query.MsisdnAsset.AsNoTracking().AnyAsync(m => !m.IsDeleted && m.Msisdn == num))
            {
                var entity = new MsisdnAsset
                {
                    Msisdn = num,
                    PoolStatus = MsisdnPoolStatus.Available,
                    ProductId = productIds[rnd.Next(productIds.Count)]
                };
                await _msisdnRepository.CreateAsync(entity);
            }
        }

        // 2. Quarantined Numbers (3 numbers: 1 past, 2 future)
        var quarantineOffsets = new[] { -5, 10, 30 }; // days
        for (int i = 0; i < 3; i++)
        {
            var num = $"093555000{i + 1}";
            if (!await _query.MsisdnAsset.AsNoTracking().AnyAsync(m => !m.IsDeleted && m.Msisdn == num))
            {
                var entity = new MsisdnAsset
                {
                    Msisdn = num,
                    PoolStatus = MsisdnPoolStatus.Quarantined,
                    QuarantineEndsUtc = DateTime.UtcNow.AddDays(quarantineOffsets[i]),
                    ProductId = productIds[rnd.Next(productIds.Count)]
                };
                await _msisdnRepository.CreateAsync(entity);
            }
        }

        // 3. Reserved Numbers (5 numbers: 3 expired > 1 hour, 2 valid)
        var reservedOffsets = new[] { -2, -1.5, -1.1, 0.5, 1.0 }; // hours
        for (int i = 0; i < 5; i++)
        {
            var num = $"093999000{i + 1}";
            if (!await _query.MsisdnAsset.AsNoTracking().AnyAsync(m => !m.IsDeleted && m.Msisdn == num))
            {
                var entity = new MsisdnAsset
                {
                    Msisdn = num,
                    PoolStatus = MsisdnPoolStatus.Reserved,
                    ProductId = productIds[rnd.Next(productIds.Count)]
                };
                await _msisdnRepository.CreateAsync(entity);
            }
        }

        await _unitOfWork.SaveAsync();

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
        await _customerRepository.CreateAsync(heroCustomer);

        var debtCustomer = IndividualCustomer.Create(
            "مازن المديون",
            _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST"),
            "0108877665",
            new PostalAddress("دمشق القديمة", "دمشق", "دمشق", "00000", "سوريا"),
            "mazen.demo@syriatel-demo.local",
            TelecomDemoMsisdn.DebtSubscriber,
            groups[rnd.Next(groups.Count)],
            categories[rnd.Next(categories.Count)]);
        debtCustomer.SetDescription("مشترك عليه ديون — سيناريو TakeOver.");
        await _customerRepository.CreateAsync(debtCustomer);

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
            await _customerRepository.CreateAsync(c);
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

                var isPrepaid = profileIndex % 2 != 0;

                var profile = new SubscriberProfile
                {
                    CustomerId = cust.Id,
                    ServiceLineType = ServiceLineType.Mobile,
                    OperationalStatus = SubscriberOperationalStatus.Active,
                    ActivationDateUtc = DateTime.UtcNow.AddDays(-rnd.Next(30, 800)),
                    LoyaltyPoints = rnd.Next(1000, 25000),
                    LoyaltyTier = rnd.Next(3) == 0 ? "Platinum" : rnd.Next(2) == 0 ? "Gold" : "Silver",
                    PrepaidBalance = isPrepaid ? rnd.Next(12500, 85000) : null,
                    PostpaidCreditLimit = !isPrepaid ? rnd.Next(50000, 300000) : null,
                    ChurnRiskScore = rnd.Next(5, 45)
                };

                if (isHero)
                {
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

                var asset = new MsisdnAsset
                {
                    Msisdn = msisdn,
                    PoolStatus = MsisdnPoolStatus.Active,
                    SubscriberProfileId = profile.Id,
                    ProductId = productIds[rnd.Next(productIds.Count)],
                    PairedIccid = kitIccid,
                    PairedImsi = kitImsi,
                };
                await _msisdnRepository.CreateAsync(asset);

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
                    ProductId = isHero && mixOffering?.ProductId != null
                        ? mixOffering.ProductId
                        : asset.ProductId,
                    ProductOfferingId = isHero && profileIndex == 0 ? mixOffering?.Id : null,
                    SubscriptionTypeId = isHero
                        ? TelecomSubscriptionTypeWellKnownIds.Hybrid
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
                            },
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
                    var sim = SimInventory.Create(kitIccid, imsi: kitImsi);
                    sim.AssignToProfile(profile.Id);
                    sim.TransitionTo(SimStatus.Active);
                    await _simRepository.CreateAsync(sim);
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
            "سعدون الشامي",
            SuspensionWellKnown.Fraud,
            "ديمو RCN: خط موقوف احتيال — يتطلب اعتماد باك أوفيس.");

        await EnsureSuspendedReconnectDemoLineAsync(
            TelecomDemoMsisdn.DebtSubscriber,
            "مازن المديون",
            SuspensionWellKnown.Billing,
            "ديمو RCN: خط موقوف فواتير — مسار تسوية Payment بالمعرض.");
    }

    private async Task EnsureSuspendedReconnectDemoLineAsync(
        string msisdn,
        string customerNameContains,
        string suspensionType,
        string susNotes)
    {
        var customerId = await ResolveReconnectDemoCustomerIdAsync(msisdn, customerNameContains);
        if (customerId == null)
        {
            return;
        }

        var assetRow = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
            .Where(m => m.Msisdn == msisdn)
            .Select(m => new { m.Id, m.SubscriberProfileId, m.PoolStatus })
            .FirstOrDefaultAsync();

        string profileId;
        string assetId;

        if (assetRow == null)
        {
            var profile = new SubscriberProfile
            {
                CustomerId = customerId,
                ServiceLineType = ServiceLineType.Mobile,
                OperationalStatus = SubscriberOperationalStatus.Suspended,
                ActivationDateUtc = DateTime.UtcNow.AddDays(-400),
            };
            profile.Suspend();
            await _profileRepository.CreateAsync(profile);
            await _unitOfWork.SaveAsync();
            profileId = profile.Id;

            var productId = await _query.Product.AsNoTracking().IsDeletedEqualTo()
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var asset = new MsisdnAsset
            {
                Msisdn = msisdn,
                SubscriberProfileId = profileId,
                ProductId = productId,
                Category = MsisdnCategory.Normal,
            };
            asset.TransitionTo(MsisdnPoolStatus.Active);
            asset.TransitionTo(MsisdnPoolStatus.Suspended);
            await _msisdnRepository.CreateAsync(asset);
            await _unitOfWork.SaveAsync();
            assetId = asset.Id;

            if (productId != null)
            {
                await _subscriptionRepository.CreateAsync(new TelecomSubscription
                {
                    SubscriberProfileId = profileId,
                    MsisdnAssetId = assetId,
                    ProductId = productId,
                    SubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Prepaid,
                    DocumentStatus = TelecomDocumentStatus.Verified,
                    IsPrimaryLine = msisdn != TelecomDemoMsisdn.ReconnectFraudDemo,
                });
                await _unitOfWork.SaveAsync();
            }
        }
        else
        {
            profileId = assetRow.SubscriberProfileId!;
            assetId = assetRow.Id;

            var profile = await _profileRepository.GetAsync(profileId, CancellationToken.None);
            if (profile != null
                && profile.OperationalStatus is not (
                    SubscriberOperationalStatus.Suspended
                    or SubscriberOperationalStatus.SuspendedInbound
                    or SubscriberOperationalStatus.SuspendedOutbound
                    or SubscriberOperationalStatus.Terminated))
            {
                profile.Suspend();
                _profileRepository.Update(profile);
            }

            var asset = await _msisdnRepository.GetAsync(assetId, CancellationToken.None);
            if (asset != null && asset.PoolStatus == MsisdnPoolStatus.Active)
            {
                asset.TransitionTo(MsisdnPoolStatus.Suspended);
                _msisdnRepository.Update(asset);
            }

            await _unitOfWork.SaveAsync();
        }

        var hasCompletedSus = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(o => o.Kind == TelecomOperationKind.TemporarySuspension
                           && o.MsisdnAssetId == assetId
                           && o.Status == TelecomOperationStatus.Completed
                           && o.SuspensionType == suspensionType);

        if (hasCompletedSus)
        {
            return;
        }

        var demoNow = DateTime.UtcNow;
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
            SuspensionStartDateUtc = demoNow.AddDays(-14),
            BarStatus = "Active",
            ConfirmedAtUtc = demoNow.AddDays(-14),
            CreatedAtUtc = demoNow.AddDays(-15),
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

    private async Task<string?> ResolveReconnectDemoCustomerIdAsync(string msisdn, string customerNameContains)
    {
        if (msisdn == TelecomDemoMsisdn.ReconnectFraudDemo)
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
        }

        return await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .Where(c => c.DisplayName.Contains(customerNameContains))
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
    }
}
