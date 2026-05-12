using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>100+ subscriber 360° rows + hero «سعدون الشامي» + demo MGR operation.</summary>
public class TelecomSyriatelSeeder
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<Customer> _customerRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IUnitOfWork _unitOfWork;

    public TelecomSyriatelSeeder(
        IQueryContext query,
        ICommandRepository<Customer> customerRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        NumberSequenceService numberSequenceService,
        IUnitOfWork unitOfWork)
    {
        _query = query;
        _customerRepository = customerRepository;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _subscriptionRepository = subscriptionRepository;
        _operationRepository = operationRepository;
        _numberSequenceService = numberSequenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        if (await _query.SubscriberProfile.AnyAsync())
            return;

        var productIds = await _query.Product
            .AsNoTracking()
            .Where(p => p.Physical == false && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .Select(p => p.Id)
            .Take(6)
            .ToListAsync();

        if (!productIds.Any())
            return;

        var rnd = new Random(20260512);
        var groups = await _query.CustomerGroup.AsNoTracking().Select(x => x.Id).ToListAsync();
        var categories = await _query.CustomerCategory.AsNoTracking().Select(x => x.Id).ToListAsync();

        var heroCustomer = new Customer
        {
            Name = "سعدون الشامي",
            Number = _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST"),
            CustomerGroupId = groups[rnd.Next(groups.Count)],
            CustomerCategoryId = categories[rnd.Next(categories.Count)],
            City = "دمشق",
            Street = "مشروع دمر — سكن جديد (بيت مع راوتر 5G)",
            State = "دمشق",
            ZipCode = "22000",
            PhoneNumber = "0939000001",
            Country = "سوريا",
            Description = "عميل منذ نحو 10 سنوات — سيناريو ديمو: تحويل شحن إلى فاتورة + راوتر للبيت الجديد.",
            EmailAddress = "sadoun.alshami@syriatelecom-demo.local"
        };
        await _customerRepository.CreateAsync(heroCustomer);

        var extraNames = new[]
        {
            "ليلى الحسين", "طارق النابلسي", "رنا دمشقية", "كريم الشوفي", "منى طرطوس",
            "علي المقداد", "هبة السويداء", "مازن درعا", "سلمى الرقة", "بشار الحسكة"
        };

        for (var i = 0; i < 79; i++)
        {
            var prefix = rnd.Next(2) == 0 ? "093" : "099";
            var c = new Customer
            {
                Name = $"{extraNames[i % extraNames.Length]} — {100 + i}",
                Number = _numberSequenceService.GenerateNumber(nameof(Customer), "", "CST"),
                CustomerGroupId = groups[rnd.Next(groups.Count)],
                CustomerCategoryId = categories[rnd.Next(categories.Count)],
                City = new[] { "دمشق", "حلب", "حمص", "اللاذقية", "حماة" }[rnd.Next(5)],
                Street = $"شارع ديمو {i}",
                State = "سوريا",
                ZipCode = $"{2000 + rnd.Next(7000)}",
                PhoneNumber = $"{prefix}{rnd.Next(1000000, 9999999)}",
                Country = "سوريا",
                Description = "مشترك مولّد آلياً لعرض OSS/BSS.",
                EmailAddress = $"bulk{i}@syriatelecom-demo.local"
            };
            await _customerRepository.CreateAsync(c);
        }

        await _unitOfWork.SaveAsync();

        var allCustomers = await _query.Customer.AsNoTracking().Where(x => !x.IsDeleted).ToListAsync();

        SubscriberProfile? heroProfile = null;
        string? heroMsisdnAssetId = null;

        foreach (var cust in allCustomers)
        {
            var msisdn = string.IsNullOrWhiteSpace(cust.PhoneNumber)
                ? $"093{Math.Abs(cust.Id.GetHashCode()) % 10_000_000:0000000}"
                : cust.PhoneNumber!.Replace(" ", "").Trim();

            var profile = new SubscriberProfile
            {
                CustomerId = cust.Id,
                NationalId = $"01{rnd.Next(10000000, 99999999)}",
                LoyaltyPoints = rnd.Next(200, 15000),
                LoyaltyTier = rnd.Next(3) == 0 ? "Gold" : rnd.Next(2) == 0 ? "Silver" : "Bronze",
                PrepaidBalance = rnd.Next(3) == 0 ? null : rnd.Next(5, 800),
                PostpaidCreditLimit = rnd.Next(4) == 0 ? rnd.Next(200, 2000) : null,
                ChurnRiskScore = rnd.Next(5, 85)
            };

            if (cust.Name != null && cust.Name.Contains("سعدون الشامي", StringComparison.Ordinal))
            {
                profile.LoyaltyPoints = 24000;
                profile.LoyaltyTier = "Platinum";
                profile.ChurnRiskScore = 12;
                profile.PrepaidBalance = 120;
                profile.PostpaidCreditLimit = 3500;
            }

            await _profileRepository.CreateAsync(profile);
            await _unitOfWork.SaveAsync();

            var asset = new MsisdnAsset
            {
                Msisdn = msisdn,
                Iccid = $"8935303{rnd.Next(100000000, 999999999)}",
                Imsi = $"41701{rnd.Next(100000000, 999999999)}",
                Puk1 = $"{rnd.Next(10000000, 99999999)}",
                Puk2 = $"{rnd.Next(10000000, 99999999)}",
                PoolStatus = MsisdnPoolStatus.Active,
                SubscriberProfileId = profile.Id,
                ProductId = productIds[rnd.Next(productIds.Count)]
            };
            await _msisdnRepository.CreateAsync(asset);

            if (cust.Name?.Contains("سعدون الشامي", StringComparison.Ordinal) == true)
            {
                heroProfile = profile;
                heroMsisdnAssetId = asset.Id;
            }

            var sub = new TelecomSubscription
            {
                SubscriberProfileId = profile.Id,
                MsisdnAssetId = asset.Id,
                ProductId = asset.ProductId,
                SubscriptionType = cust.Name?.Contains("سعدون", StringComparison.Ordinal) == true
                    ? TelecomSubscriptionType.Hybrid
                    : rnd.Next(3) switch { 0 => TelecomSubscriptionType.Prepaid, 1 => TelecomSubscriptionType.Postpaid, _ => TelecomSubscriptionType.Hybrid },
                DocumentStatus = TelecomDocumentStatus.Verified,
                IsPrimaryLine = true
            };
            await _subscriptionRepository.CreateAsync(sub);

            await _unitOfWork.SaveAsync();
        }

        if (heroProfile != null && !string.IsNullOrEmpty(heroMsisdnAssetId))
        {
            var (entityName, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.Migration);
            var op = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.Migration,
                Number = _numberSequenceService.GenerateNumber(entityName, prefix, "", useDate: false),
                Status = TelecomOperationStatus.Draft,
                DocumentStatus = TelecomDocumentStatus.Uploaded,
                SubscriberProfileId = heroProfile.Id,
                MsisdnAssetId = heroMsisdnAssetId,
                Notes = "ديمو: تحويل من شحن إلى فاتورة + طلب راوتر 5G لمشروع دمر — جاهز للتأكيد وعرض Polly/CBS."
            };
            await _operationRepository.CreateAsync(op);
            await _unitOfWork.SaveAsync();
        }
    }
}
