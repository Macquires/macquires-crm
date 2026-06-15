using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Security;
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
    private readonly INationalIdSearchHashBackfillService _nationalIdBackfill;
    private readonly IFieldEncryptionService _encryption;

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
        IUnitOfWork unitOfWork,
        INationalIdSearchHashBackfillService nationalIdBackfill,
        IFieldEncryptionService encryption)
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
        _nationalIdBackfill = nationalIdBackfill;
        _encryption = encryption;
    }

    public async Task EnsureEnrichedAsync()
    {
        await _nationalIdBackfill.BackfillAllMissingAsync();

        var customers = await _context.Customer.Where(c => !c.IsDeleted).ToListAsync();

        await SanitizeOperatorCreatedProfilesAsync(customers);
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
            if (IsOperatorCreatedCustomer(customer))
            {
                continue;
            }

            var seed = StableHash(customer.Id);
            var rnd = new Random(seed);

            if (DemoSyrianSubscriberCatalog.IsPlaceholderDisplayName(customer.DisplayName))
            {
                customer.SetDisplayName(DemoSyrianSubscriberCatalog.GetName(nameSlot++));
            }

            EnrichPartyFields(customer, i, rnd);
            await EnsureContactsAsync(customer, rnd);
            
            // Save every 20 customers to avoid huge transactions but keep it fast
            if (i % 20 == 0)
            {
                await _context.SaveChangesAsync();
            }
        }

        await _context.SaveChangesAsync();

        await EnsureTelecomStacksAsync(customers, productIds, vasCatalog);
        await EnsureDiverseProductsOnLinesAsync(productsByType, productIds);
        await EnsureTicketsAsync(customers);
        await EnsureOperationsAndBillingAsync(customers);
        await EnsureAuditTrailAsync(customers);
    }

    /// <summary>Removes demo wallet/loyalty fields wrongly applied before operator-skip was enforced.</summary>
    private async Task SanitizeOperatorCreatedProfilesAsync(IReadOnlyList<Customer> customers)
    {
        foreach (var customer in customers.Where(IsOperatorCreatedCustomer))
        {
            var profiles = await _context.SubscriberProfile
                .Where(p => !p.IsDeleted && p.CustomerId == customer.Id)
                .ToListAsync();

            foreach (var profile in profiles)
            {
                var hasPostedPayment = await _context.TelecomPaymentTransaction.AsNoTracking()
                    .AnyAsync(p => !p.IsDeleted
                                   && p.SubscriberProfileId == profile.Id
                                   && p.Status == PaymentTransactionStatus.Completed);

                if (hasPostedPayment)
                {
                    continue;
                }

                profile.PrepaidBalance = null;
                profile.PostpaidCreditLimit = null;
                profile.LoyaltyTier = null;
                profile.LoyaltyPoints = 0;
                profile.ChurnRiskScore = null;
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>POS / Hub customers created by a real operator — keep only what they entered.</summary>
    private static bool IsOperatorCreatedCustomer(Customer customer)
    {
        var createdBy = customer.CreatedById;
        if (string.IsNullOrWhiteSpace(createdBy))
        {
            return false;
        }

        return !string.Equals(createdBy, SystemActor, StringComparison.OrdinalIgnoreCase);
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

    private void EnrichPartyFields(Customer customer, int index, Random rnd)
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
            individual.SyncNationalIdSearchHash(_encryption);
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
            var contactNumber = await _numberSequence.GenerateNumberAsync(nameof(CustomerContact), "", "CC");
            await _contactRepository.CreateAsync(new CustomerContact
            {
                CustomerId = customer.Id,
                Name = first,
                Number = contactNumber,
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
        var i = 0;
        foreach (var customer in customers)
        {
            if (IsOperatorCreatedCustomer(customer))
            {
                continue;
            }

            var profiles = await _context.SubscriberProfile
                .Include(p => p.Subscriptions)
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
            if (++i % 20 == 0)
            {
                await _context.SaveChangesAsync();
            }
            await EnsureVasForCustomerAsync(customer.Id, vasCatalog, rnd);
        }
        await _context.SaveChangesAsync();
    }

    private static void EnrichProfile(SubscriberProfile profile, Random rnd)
    {
        profile.ActivationDateUtc ??= DateTime.UtcNow.AddDays(-rnd.Next(60, 900));
        profile.LoyaltyTier ??= rnd.Next(3) switch { 0 => "Platinum", 1 => "Gold", _ => "Silver" };
        profile.LoyaltyPoints = profile.LoyaltyPoints <= 0 ? rnd.Next(2000, 35000) : profile.LoyaltyPoints;
        profile.ChurnRiskScore ??= rnd.Next(5, 40);

        var subscriptionTypeId = profile.Subscriptions.FirstOrDefault()?.SubscriptionTypeId;
        var isPrepaid = subscriptionTypeId == TelecomSubscriptionTypeWellKnownIds.Prepaid;
        var isPostpaid = subscriptionTypeId == TelecomSubscriptionTypeWellKnownIds.Postpaid;
        var isHybrid = subscriptionTypeId == TelecomSubscriptionTypeWellKnownIds.Hybrid || subscriptionTypeId == null;

        var isSuspended = profile.OperationalStatus is SubscriberOperationalStatus.Suspended
            or SubscriberOperationalStatus.SuspendedInbound
            or SubscriberOperationalStatus.SuspendedOutbound;

        if (isSuspended)
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
            profile.PrepaidBalance = (isPrepaid || isHybrid) ? rnd.Next(8000, 95000) : null;
            profile.PostpaidCreditLimit = (isPostpaid || isHybrid) ? rnd.Next(80000, 350000) : null;
        }

        profile.LanguagePreference = profile.LanguagePreference == LanguagePreference.Arabic && rnd.Next(5) == 0
            ? LanguagePreference.English
            : LanguagePreference.Arabic;
        
        if (profile.OperationalStatus == SubscriberOperationalStatus.Pending)
        {
            profile.OperationalStatus = SubscriberOperationalStatus.Active;
        }
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
            .Where(s => s.SubscriberProfile != null && s.SubscriberProfile.CustomerId == customerId && s.MsisdnAssetId != null)
            .Select(s => new { s.Id, Msisdn = s.MsisdnAsset!.Msisdn })
            .ToListAsync();

        if (subs.Count == 0) return;

        var subIds = subs.Select(s => s.Id).ToList();
        var existingVas = await _query.SubscriberActiveService.AsNoTracking()
            .Where(v => !v.IsDeleted && subIds.Contains(v.TelecomSubscriptionId))
            .ToListAsync();

        var existingBySub = existingVas.GroupBy(v => v.TelecomSubscriptionId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.TelecomValueAddedServiceId)
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(id => id!)
                    .ToHashSet());

        foreach (var sub in subs)
        {
            var existingSet = existingBySub.GetValueOrDefault(sub.Id) ?? new HashSet<string>();
            var existingCount = existingSet.Count;

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
                .Where(v => !existingSet.Contains(v.Id))
                .Take(target - existingCount);

            foreach (var vas in picked)
            {
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
        var operatorCustomerIds = await _context.Customer.AsNoTracking()
            .Where(c => !c.IsDeleted
                        && c.CreatedById != null
                        && c.CreatedById != ""
                        && c.CreatedById != SystemActor)
            .Select(c => c.Id)
            .ToHashSetAsync();

        var subs = await _subscriptionRepository.GetQuery()
            .Include(s => s.SubscriberProfile)
            .Where(s => !s.IsDeleted && s.MsisdnAssetId != null)
            .ToListAsync();

        foreach (var sub in subs)
        {
            if (sub.SubscriberProfile != null && operatorCustomerIds.Contains(sub.SubscriberProfile.CustomerId))
            {
                continue;
            }

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
            if (IsOperatorCreatedCustomer(customer))
            {
                continue;
            }

            var hasTicket = await _query.TelecomTechnicalTicket.AsNoTracking()
                .AnyAsync(t => !t.IsDeleted && t.CustomerId == customer.Id);
            if (hasTicket)
            {
                continue;
            }

            var line = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                .Where(s => s.SubscriberProfile != null && s.SubscriberProfile.CustomerId == customer.Id && s.MsisdnAsset != null)
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
            var ticketNumber = await _numberSequence.GenerateNumberAsync(nameof(TelecomTechnicalTicket), "", "TT");
            await _ticketRepository.CreateAsync(new TelecomTechnicalTicket
            {
                CreatedById = SystemActor,
                TicketNumber = ticketNumber,
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
            if (IsOperatorCreatedCustomer(customer))
            {
                continue;
            }

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
            var operationNumber = await _numberSequence.GenerateNumberAsync(entityName, prefix, "", useDate: false);
            var op = new TelecomOperationRequest
            {
                Kind = TelecomOperationKind.ServiceModification,
                Number = operationNumber,
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
                BranchId = op.BranchId,
            });
        }

        await _unitOfWork.SaveAsync();
    }

    private async Task EnsureAuditTrailAsync(IReadOnlyList<Customer> customers)
    {
        var existingEntityIds = await _context.UserAuditLog.AsNoTracking()
            .Where(x => !x.IsDeleted && x.EntityType == nameof(Customer))
            .Select(x => x.EntityId)
            .Distinct()
            .ToListAsync();

        var existingSet = new HashSet<string>(existingEntityIds.Where(id => id != null)!);

        var i = 0;
        foreach (var customer in customers)
        {
            if (IsOperatorCreatedCustomer(customer))
            {
                continue;
            }

            if (existingSet.Contains(customer.Id))
            {
                continue;
            }

            var msisdn = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                .Where(s => s.SubscriberProfile != null && s.SubscriberProfile.CustomerId == customer.Id && s.MsisdnAsset != null)
                .OrderByDescending(s => s.IsPrimaryLine)
                .Select(s => s.MsisdnAsset!.Msisdn)
                .FirstOrDefaultAsync();

            var actions = new[]
            {
                (UserAuditActionTypes.CustomerCreated, $"إنشاء ملف مشترك: {customer.DisplayName}"),
                (UserAuditActionTypes.CustomerUpdated, $"تحديث بيانات التواصل — {customer.DisplayName}"),
                (UserAuditActionTypes.CustomerViewed, $"اطلاع على ملف 360 — {customer.DisplayName}"),
            };

            foreach (var (action, summary) in actions)
            {
                // GLOBAL HARDENING: Manual row creation to avoid SaveChangesAsync inside loop
                var ip = "127.0.0.1";
                var row = new UserAuditLog
                {
                    UserId = null,
                    ActorUserId = SystemActor,
                    ActionType = action,
                    EntityType = nameof(Customer),
                    EntityId = customer.Id,
                    SummaryAr = summary,
                    PayloadJson = UserAuditJsonSerializer.Serialize(AuditLogPayloadFactory.ProfileView(customer.Id, customer.DisplayName, msisdn ?? customer.PrimaryPhone)),
                    OccurredAtUtc = DateTime.UtcNow,
                    IpAddress = ip,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedById = SystemActor,
                    IsDeleted = false,
                };
                await _context.UserAuditLog.AddAsync(row);
            }

            if (++i % 20 == 0)
            {
                await _context.SaveChangesAsync();
            }
        }
        await _context.SaveChangesAsync();
    }
}
