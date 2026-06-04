using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Features.NumberSequenceManager;
using Application.Features.TelecomManager.Commands;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.SeedManager.Demos;

/// <summary>
/// Backfills every <see cref="Customer"/> and telecom line so Customer 360 shows complete, realistic Syrian demo data.
/// Safe to run on every startup (idempotent).
/// </summary>
public sealed class TelecomCustomer360EnrichmentSeeder
{
    private const string SystemActor = "system-seed";

    private readonly DataContext _context;
    private readonly IQueryContext _query;
    private readonly ICommandRepository<CustomerContact> _contactRepository;
    private readonly ICommandRepository<SubscriberProfile> _profileRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<TelecomSubscription> _subscriptionRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ICommandRepository<BillingIntegrationLog> _logRepository;
    private readonly ICommandRepository<SubscriberActiveService> _vasActiveRepository;
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepository;
    private readonly NumberSequenceService _numberSequence;
    private readonly IUserAuditService _audit;
    private readonly IUnitOfWork _unitOfWork;

    public TelecomCustomer360EnrichmentSeeder(
        DataContext context,
        IQueryContext query,
        ICommandRepository<CustomerContact> contactRepository,
        ICommandRepository<SubscriberProfile> profileRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<TelecomSubscription> subscriptionRepository,
        ICommandRepository<SimInventory> simRepository,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<BillingIntegrationLog> logRepository,
        ICommandRepository<SubscriberActiveService> vasActiveRepository,
        ICommandRepository<TelecomTechnicalTicket> ticketRepository,
        NumberSequenceService numberSequence,
        IUserAuditService audit,
        IUnitOfWork unitOfWork)
    {
        _context = context;
        _query = query;
        _contactRepository = contactRepository;
        _profileRepository = profileRepository;
        _msisdnRepository = msisdnRepository;
        _subscriptionRepository = subscriptionRepository;
        _simRepository = simRepository;
        _operationRepository = operationRepository;
        _logRepository = logRepository;
        _vasActiveRepository = vasActiveRepository;
        _ticketRepository = ticketRepository;
        _numberSequence = numberSequence;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task EnsureEnrichedAsync()
    {
        var customers = await _context.Customer.Where(c => !c.IsDeleted).ToListAsync();
        if (customers.Count == 0)
        {
            return;
        }

        var products = await _query.Product.AsNoTracking()
            .Where(p => p.Physical != true && !p.IsDeleted && p.ServiceCode != null)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var productIds = products.Select(p => p.Id).ToList();
        var productsByType = products
            .GroupBy(p => p.CompatibleSubscriptionTypeId ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

        if (productIds.Count == 0)
        {
            return;
        }

        var vasCatalog = await _query.TelecomValueAddedService.AsNoTracking()
            .Where(v => !v.IsDeleted && v.IsActive)
            .OrderBy(v => v.SortOrder)
            .ToListAsync();

        var nameSlot = 0;
        for (var i = 0; i < customers.Count; i++)
        {
            var customer = customers[i];
            var seed = StableHash(customer.Id);
            var rnd = new Random(seed);

            if (DemoSyrianSubscriberCatalog.IsPlaceholderDisplayName(customer.DisplayName))
            {
                customer.SetDisplayName(DemoSyrianSubscriberCatalog.GetName(nameSlot++));
            }

            EnrichPartyFields(customer, i, rnd);
            await EnsureContactsAsync(customer, rnd);
        }

        await _context.SaveChangesAsync();

        await EnsureTelecomStacksAsync(customers, productIds, vasCatalog);
        await EnsureDiverseProductsOnLinesAsync(productsByType, productIds);
        await EnsureTicketsAsync(customers);
        await EnsureOperationsAndBillingAsync(customers);
        await EnsureAuditTrailAsync(customers);
    }

    private static int StableHash(string id)
    {
        unchecked
        {
            var h = 17;
            foreach (var ch in id)
            {
                h = (h * 31) + ch;
            }

            return Math.Abs(h);
        }
    }

    private static void EnrichPartyFields(Customer customer, int index, Random rnd)
    {
        var city = DemoSyrianSubscriberCatalog.Cities[index % DemoSyrianSubscriberCatalog.Cities.Length];
        var street = DemoSyrianSubscriberCatalog.Streets[rnd.Next(DemoSyrianSubscriberCatalog.Streets.Length)];
        var zip = $"{10000 + rnd.Next(9000)}";

        if (string.IsNullOrWhiteSpace(customer.Address.Street) || customer.Address.Street == "شارع تجريبي")
        {
            customer.UpdateAddress(new PostalAddress(street, city, city, zip, "سوريا"));
        }

        var slug = customer.DisplayName.Replace(" ", ".", StringComparison.Ordinal);
        var email = string.IsNullOrWhiteSpace(customer.ContactEmail) || customer.ContactEmail.Contains("bulk", StringComparison.OrdinalIgnoreCase)
            ? $"{slug.ToLowerInvariant()}@syriatel-demo.local"
            : customer.ContactEmail;

        var phone = string.IsNullOrWhiteSpace(customer.PrimaryPhone)
            ? $"09{rnd.Next(3, 10)}{rnd.Next(1000000, 9999999)}"
            : customer.PrimaryPhone;

        var fax = customer.FaxNumber ?? $"011{rnd.Next(2000000, 2999999)}";
        var website = customer.Website ?? $"https://{slug.ToLowerInvariant()}.syriatel-demo.local";

        customer.UpdateContact(email, phone, fax, website);
        customer.UpdateSocial(
            customer.WhatsApp ?? phone,
            customer.LinkedIn ?? $"linkedin.com/in/{slug.ToLowerInvariant()}",
            customer.Facebook ?? $"facebook.com/{slug.ToLowerInvariant()}",
            customer.Instagram ?? $"@{slug.ToLowerInvariant()}",
            customer.TwitterX ?? $"@{slug.ToLowerInvariant()}",
            customer.TikTok ?? $"@{slug.ToLowerInvariant()}");

        if (string.IsNullOrWhiteSpace(customer.Description))
        {
            customer.SetDescription($"مشترك سيريتل نشط منذ {2015 + rnd.Next(8)} — بيانات ديمو كاملة للعرض التشغيلي.");
        }

        if (customer is IndividualCustomer individual)
        {
            var birthYear = 1975 + rnd.Next(25);
            var birthMonth = rnd.Next(1, 13);
            var birthDay = rnd.Next(1, 28);
            individual.UpdateIdentity(
                individual.NationalId.Length >= 8 ? individual.NationalId : $"010{rnd.Next(10000000, 99999999)}",
                individual.DateOfBirth ?? new DateOnly(birthYear, birthMonth, birthDay),
                individual.Nationality ?? "سورية",
                individual.Gender == Gender.Unknown ? (rnd.Next(2) == 0 ? Gender.Male : Gender.Female) : individual.Gender,
                individual.Occupation ?? DemoSyrianSubscriberCatalog.Occupations[rnd.Next(DemoSyrianSubscriberCatalog.Occupations.Length)]);
        }
        else if (customer is CorporateCustomer corporate)
        {
            corporate.UpdateCorporateIdentity(
                corporate.CommercialRegistryNumber,
                corporate.TaxNumber ?? $"TAX-{rnd.Next(10000, 99999)}",
                corporate.AuthorizedSignatoryName ?? "المفوّض بالتوقيع — إدارة العقود",
                corporate.LegalStatus == CompanyLegalStatus.Unknown
                    ? CompanyLegalStatus.LimitedLiability
                    : corporate.LegalStatus);
        }
    }

    private async Task EnsureContactsAsync(Customer customer, Random rnd)
    {
        var existing = await _context.CustomerContact
            .Where(c => !c.IsDeleted && c.CustomerId == customer.Id)
            .ToListAsync();

        foreach (var contact in existing)
        {
            if (string.IsNullOrWhiteSpace(contact.Description))
            {
                contact.Description = DemoSyrianSubscriberCatalog.ContactNotes[rnd.Next(DemoSyrianSubscriberCatalog.ContactNotes.Length)];
            }
        }

        var needed = Math.Max(0, 3 - existing.Count);
        for (var i = 0; i < needed; i++)
        {
            var first = DemoSyrianSubscriberCatalog.FullNames[rnd.Next(DemoSyrianSubscriberCatalog.FullNames.Length)];
            var prefix = rnd.Next(2) == 0 ? "093" : "099";
            await _contactRepository.CreateAsync(new CustomerContact
            {
                CustomerId = customer.Id,
                Name = first,
                Number = _numberSequence.GenerateNumber(nameof(CustomerContact), "", "CC"),
                JobTitle = DemoSyrianSubscriberCatalog.Occupations[rnd.Next(DemoSyrianSubscriberCatalog.Occupations.Length)],
                EmailAddress = $"contact.{rnd.Next(100000, 999999)}@syriatel-demo.local",
                PhoneNumber = $"{prefix}{rnd.Next(1000000, 9999999)}",
                Description = DemoSyrianSubscriberCatalog.ContactNotes[rnd.Next(DemoSyrianSubscriberCatalog.ContactNotes.Length)],
            });
        }
    }

    private async Task EnsureTelecomStacksAsync(
        IReadOnlyList<Customer> customers,
        IReadOnlyList<string> productIds,
        IReadOnlyList<TelecomValueAddedService> vasCatalog)
    {
        var rnd = new Random(20260519);
        foreach (var customer in customers)
        {
            var profiles = await _context.SubscriberProfile
                .Where(p => !p.IsDeleted && p.CustomerId == customer.Id)
                .ToListAsync();

            if (profiles.Count == 0)
            {
                await CreateTelecomStackAsync(customer, productIds, rnd);
                continue;
            }

            foreach (var profile in profiles)
            {
                EnrichProfile(profile, rnd);
            }

            // Profiles were loaded on DataContext — persist on the same tracker (not CommandContext).
            await _context.SaveChangesAsync();
            await EnsureVasForCustomerAsync(customer.Id, vasCatalog, rnd);
        }
    }

    private static void EnrichProfile(SubscriberProfile profile, Random rnd)
    {
        profile.ActivationDateUtc ??= DateTime.UtcNow.AddDays(-rnd.Next(60, 900));
        profile.LoyaltyTier ??= rnd.Next(3) switch { 0 => "Platinum", 1 => "Gold", _ => "Silver" };
        profile.LoyaltyPoints = profile.LoyaltyPoints <= 0 ? rnd.Next(2000, 35000) : profile.LoyaltyPoints;
        profile.ChurnRiskScore ??= rnd.Next(5, 40);
        profile.PrepaidBalance ??= rnd.Next(8000, 95000);
        profile.PostpaidCreditLimit ??= rnd.Next(80000, 350000);
        profile.LanguagePreference = profile.LanguagePreference == LanguagePreference.Arabic && rnd.Next(5) == 0
            ? LanguagePreference.English
            : LanguagePreference.Arabic;
        profile.OperationalStatus = SubscriberOperationalStatus.Active;
    }

    private async Task CreateTelecomStackAsync(Customer customer, IReadOnlyList<string> productIds, Random rnd)
    {
        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(customer.PrimaryPhone)
            ?? $"09{rnd.Next(3, 10)}{rnd.Next(1000000, 9999999)}";

        var taken = await _query.MsisdnAsset.AsNoTracking().AnyAsync(m => !m.IsDeleted && m.Msisdn == msisdn);
        if (taken)
        {
            msisdn = await GenerateUniqueMsisdnAsync(rnd);
        }

        var profile = new SubscriberProfile
        {
            CustomerId = customer.Id,
            ServiceLineType = ServiceLineType.Mobile,
            OperationalStatus = SubscriberOperationalStatus.Active,
            ActivationDateUtc = DateTime.UtcNow.AddDays(-rnd.Next(90, 700)),
            LoyaltyPoints = rnd.Next(3000, 28000),
            LoyaltyTier = "Gold",
            PrepaidBalance = rnd.Next(10000, 60000),
            PostpaidCreditLimit = rnd.Next(100000, 250000),
            ChurnRiskScore = rnd.Next(8, 35),
            LanguagePreference = LanguagePreference.Arabic,
        };
        await _profileRepository.CreateAsync(profile);
        await _unitOfWork.SaveAsync();

        var productId = productIds[rnd.Next(productIds.Count)];
        var asset = new MsisdnAsset
        {
            Msisdn = msisdn,
            PoolStatus = MsisdnPoolStatus.Active,
            SubscriberProfileId = profile.Id,
            ProductId = productId,
        };
        await _msisdnRepository.CreateAsync(asset);

        var sub = new TelecomSubscription
        {
            SubscriberProfileId = profile.Id,
            MsisdnAssetId = asset.Id,
            ProductId = productId,
            SubscriptionTypeId = TelecomSubscriptionTypeWellKnownIds.Hybrid,
            DocumentStatus = TelecomDocumentStatus.Verified,
            IsPrimaryLine = true,
        };
        await _subscriptionRepository.CreateAsync(sub);

        var iccid = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(msisdn);
        if (!string.IsNullOrWhiteSpace(iccid))
        {
            var imsi = MsisdnAssetKitResolver.DeriveImsiFromMsisdn(msisdn);
            var sim = SimInventory.Create(iccid, imsi: imsi);
            sim.AssignToProfile(profile.Id);
            sim.TransitionTo(SimStatus.Active);
            await _simRepository.CreateAsync(sim);
            asset.PairedIccid = iccid;
            asset.PairedImsi = imsi;
            // Do not call Update() here — asset is still Added; Update() would force UPDATE and fail RowVersion check.
        }

        await _unitOfWork.SaveAsync();
    }

    private async Task<string> GenerateUniqueMsisdnAsync(Random rnd)
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

        throw new InvalidOperationException("TelecomCustomer360EnrichmentSeeder: could not allocate MSISDN.");
    }

    private async Task EnsureVasForCustomerAsync(
        string customerId,
        IReadOnlyList<TelecomValueAddedService> vasCatalog,
        Random rnd)
    {
        if (vasCatalog.Count == 0)
        {
            return;
        }

        var subs = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfile.CustomerId == customerId && s.MsisdnAssetId != null)
            .Select(s => new { s.Id, Msisdn = s.MsisdnAsset!.Msisdn })
            .ToListAsync();

        foreach (var sub in subs)
        {
            var existingCount = await _query.SubscriberActiveService.AsNoTracking()
                .CountAsync(v => !v.IsDeleted && v.TelecomSubscriptionId == sub.Id);

            var target = Math.Min(5, vasCatalog.Count);
            if (existingCount >= target)
            {
                continue;
            }

            var offset = StableHash(sub.Id) % vasCatalog.Count;
            var picked = vasCatalog
                .Select((v, i) => (v, i))
                .OrderBy(x => (x.i + offset) % vasCatalog.Count)
                .Select(x => x.v)
                .Take(target - existingCount);
            foreach (var vas in picked)
            {
                var dup = await _query.SubscriberActiveService.AnyAsync(
                    v => !v.IsDeleted && v.TelecomSubscriptionId == sub.Id && v.TelecomValueAddedServiceId == vas.Id);
                if (dup)
                {
                    continue;
                }

                await _vasActiveRepository.CreateAsync(new SubscriberActiveService
                {
                    TelecomSubscriptionId = sub.Id,
                    TelecomValueAddedServiceId = vas.Id,
                    Msisdn = sub.Msisdn,
                    Status = SubscriberVasStatus.Active,
                    ActivatedAtUtc = DateTime.UtcNow.AddDays(-rnd.Next(10, 400)),
                });
            }
        }

        await _unitOfWork.SaveAsync();
    }

    /// <summary>Assigns a distinct technical product per line based on subscription type (stable hash).</summary>
    private async Task EnsureDiverseProductsOnLinesAsync(
        IReadOnlyDictionary<string, List<string>> productsByType,
        IReadOnlyList<string> allProductIds)
    {
        if (allProductIds.Count == 0)
        {
            return;
        }

        // Load via CommandContext (same tracker as _subscriptionRepository) — avoids duplicate
        // tracking when stacks were just created in EnsureTelecomStacksAsync.
        var subs = await _subscriptionRepository.GetQuery()
            .Where(s => !s.IsDeleted && s.MsisdnAssetId != null)
            .ToListAsync();

        foreach (var sub in subs)
        {
            var typeKey = sub.SubscriptionTypeId ?? string.Empty;
            var pool = productsByType.TryGetValue(typeKey, out var typed) && typed.Count > 0
                ? typed
                : allProductIds.ToList();

            var pick = pool[StableHash(sub.Id) % pool.Count];
            if (sub.ProductId == pick)
            {
                continue;
            }

            sub.ProductId = pick;
            sub.UpdatedAtUtc = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(sub.MsisdnAssetId))
            {
                var asset = await _msisdnRepository.GetAsync(sub.MsisdnAssetId, CancellationToken.None);
                if (asset != null && asset.ProductId != pick)
                {
                    asset.ProductId = pick;
                    asset.UpdatedAtUtc = DateTime.UtcNow;
                }
            }
        }

        await _unitOfWork.SaveAsync();
    }

    private async Task EnsureTicketsAsync(IReadOnlyList<Customer> customers)
    {
        var titles = new[]
        {
            ("استفسار فوترة", TechnicalTicketIssueType.Billing, TechnicalTicketPriority.Medium),
            ("ضعف تغطية 4G", TechnicalTicketIssueType.Network, TechnicalTicketPriority.High),
            ("طلب تفعيل باقة", TechnicalTicketIssueType.Provisioning, TechnicalTicketPriority.Medium),
        };

        foreach (var customer in customers)
        {
            var hasTicket = await _query.TelecomTechnicalTicket.AsNoTracking()
                .AnyAsync(t => !t.IsDeleted && t.CustomerId == customer.Id);
            if (hasTicket)
            {
                continue;
            }

            var line = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                .Where(s => s.SubscriberProfile.CustomerId == customer.Id && s.MsisdnAsset != null)
                .OrderByDescending(s => s.IsPrimaryLine)
                .Select(s => new
                {
                    s.SubscriberProfileId,
                    s.MsisdnAssetId,
                    Msisdn = s.MsisdnAsset!.Msisdn,
                })
                .FirstOrDefaultAsync();

            if (line == null)
            {
                continue;
            }

            var pick = titles[StableHash(customer.Id) % titles.Length];
            await _ticketRepository.CreateAsync(new TelecomTechnicalTicket
            {
                CreatedById = SystemActor,
                TicketNumber = _numberSequence.GenerateNumber(nameof(TelecomTechnicalTicket), "", "TT"),
                Msisdn = line.Msisdn,
                CustomerId = customer.Id,
                SubscriberProfileId = line.SubscriberProfileId,
                IssueType = pick.Item2,
                TicketCategory = TechnicalTicketCategory.Complaint,
                Priority = pick.Item3,
                Status = TechnicalTicketStatus.Open,
                Notes = pick.Item1,
                PayloadJson = $"{{\"summary\":\"{pick.Item1}\",\"msisdnAssetId\":\"{line.MsisdnAssetId}\"}}",
                OpenedByUserId = SystemActor,
                CreatedByChannel = TechnicalTicketCreatedByChannel.CallCenterAgent,
            });
        }

        await _unitOfWork.SaveAsync();
    }

    private async Task EnsureOperationsAndBillingAsync(IReadOnlyList<Customer> customers)
    {
        foreach (var customer in customers)
        {
            var profileIds = await _query.SubscriberProfile.AsNoTracking()
                .Where(p => !p.IsDeleted && p.CustomerId == customer.Id)
                .Select(p => p.Id)
                .ToListAsync();

            if (profileIds.Count == 0)
            {
                continue;
            }

            var hasOp = await _query.TelecomOperationRequest.AsNoTracking()
                .AnyAsync(o => !o.IsDeleted && profileIds.Contains(o.SubscriberProfileId));
            if (hasOp)
            {
                continue;
            }

            var line = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                .Where(s => profileIds.Contains(s.SubscriberProfileId) && s.MsisdnAssetId != null)
                .OrderByDescending(s => s.IsPrimaryLine)
                .Select(s => new { s.SubscriberProfileId, s.MsisdnAssetId })
                .FirstOrDefaultAsync();

            if (line == null || string.IsNullOrEmpty(line.MsisdnAssetId))
            {
                continue;
            }

            var (entityName, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.ServiceModification);
            var op = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.ServiceModification,
                Number = _numberSequence.GenerateNumber(entityName, prefix, "", useDate: false),
                Status = TelecomOperationStatus.Completed,
                DocumentStatus = TelecomDocumentStatus.Verified,
                SubscriberProfileId = line.SubscriberProfileId,
                MsisdnAssetId = line.MsisdnAssetId,
                Notes = $"عملية ديمو مكتملة — {customer.DisplayName}",
                IsLostOrStolenReport = false,
                FraudClearanceConfirmed = false,
                AutoReconnectEnabled = false,
                NotificationSuppressed = false,
                RequiresDualApproval = false,
            };
            await _operationRepository.CreateAsync(op);
            await _unitOfWork.SaveAsync();

            await _logRepository.CreateAsync(new BillingIntegrationLog
            {
                TelecomOperationRequestId = op.Id,
                AttemptNumber = 1,
                Success = true,
                Message = "CBS-OK-200: تمت مزامنة الرصيد والباقة بنجاح.",
                IntegrationTarget = "Huawei CBS API v2.1",
            });
        }

        await _unitOfWork.SaveAsync();
    }

    private async Task EnsureAuditTrailAsync(IReadOnlyList<Customer> customers)
    {
        foreach (var customer in customers)
        {
            var count = await _context.UserAuditLog.AsNoTracking()
                .CountAsync(x => !x.IsDeleted && x.EntityType == nameof(Customer) && x.EntityId == customer.Id);

            if (count >= 3)
            {
                continue;
            }

            var msisdn = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                .Where(s => s.SubscriberProfile.CustomerId == customer.Id && s.MsisdnAsset != null)
                .OrderByDescending(s => s.IsPrimaryLine)
                .Select(s => s.MsisdnAsset!.Msisdn)
                .FirstOrDefaultAsync();

            var actions = new[]
            {
                (UserAuditActionTypes.CustomerCreated, $"إنشاء ملف مشترك: {customer.DisplayName}"),
                (UserAuditActionTypes.CustomerUpdated, $"تحديث بيانات التواصل — {customer.DisplayName}"),
                (UserAuditActionTypes.CustomerViewed, $"اطلاع على ملف 360 — {customer.DisplayName}"),
            };

            foreach (var (action, summary) in actions.Skip(3 - count))
            {
                await _audit.LogAsync(new UserAuditLogRequest
                {
                    ActorUserId = SystemActor,
                    ActionType = action,
                    EntityType = nameof(Customer),
                    EntityId = customer.Id,
                    SummaryAr = summary,
                    Payload = AuditLogPayloadFactory.ProfileView(customer.Id, customer.DisplayName, msisdn ?? customer.PrimaryPhone),
                    IpAddress = "127.0.0.1",
                });
            }
        }
    }
}
