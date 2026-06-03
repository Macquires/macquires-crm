using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public record Customer360OperationDto(
    string Id,
    string Number,
    TelecomOperationKind Kind,
    string? KindLabelAr,
    TelecomOperationStatus Status,
    string? StatusLabelAr,
    string? TransferReason,
    string? Msisdn,
    string? CounterpartyNameAr,
    string? CorrelationId,
    DateTime? CreatedAtUtc);

public record Customer360ContactDto(
    string Id,
    string? Name,
    string? JobTitle,
    string? PhoneNumber,
    string? EmailAddress,
    string? Description);

public record Customer360PackageComponentDto(
    ServiceComponentType ComponentType,
    string? Label,
    decimal? Quota,
    string? QuotaUnit,
    bool IsUnlimited,
    int SortOrder);

public record Customer360SubscriptionDto(
    string Id,
    string SubscriberProfileId,
    string? Msisdn,
    string? MsisdnAssetId,
    bool IsPrimaryLine,
    string? ProductId,
    string? ProductName,
    string? ProductOfferingId,
    string? ProductOfferingName,
    string? SubscriptionTypeName,
    string? SubscriptionTypeCode,
    string? SimType,
    string? Iccid,
    string? ProfileOperationalStatus,
    string? DocumentStatusLabel,
    string? ServiceLineTypeLabel,
    string? LanguagePreferenceLabel,
    DateTime? ActivationDateUtc,
    int LoyaltyPoints,
    string? LoyaltyTier,
    decimal? PrepaidBalance,
    decimal? PostpaidCreditLimit,
    int? ChurnRiskScore,
    string? SimStatus,
    string? Imsi,
    DateTime? CreatedAtUtc,
    List<Customer360PackageComponentDto> PackageComponents);

public class GetCustomer360Result
{
    public string CustomerId { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string AccountNumber { get; init; } = "";
    public string? Description { get; init; }
    public CustomerKind CustomerKind { get; init; }
    /// <summary>Arabic display label for CustomerKind (not the numeric enum value).</summary>
    public string CustomerKindLabel { get; init; } = "";
    public CustomerStatus Status { get; init; }
    public string StatusLabel { get; init; } = "";
    public string? StatusReasonCodeLabel { get; init; }
    public string? StatusReasonNote { get; init; }
    public string? NationalIdMasked { get; init; }
    public string? CommercialRegistry { get; init; }
    public string? PrimaryPhone { get; init; }
    public string? ContactEmail { get; init; }
    public string? FaxNumber { get; init; }
    public string? Website { get; init; }
    public string? WhatsApp { get; init; }
    public string? LinkedIn { get; init; }
    public string? Facebook { get; init; }
    public string? Instagram { get; init; }
    public string? TwitterX { get; init; }
    public string? TikTok { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public string? CustomerGroupName { get; init; }
    public string? CustomerCategoryName { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Nationality { get; init; }
    public string? GenderLabel { get; init; }
    public string? Occupation { get; init; }
    public string? TaxNumber { get; init; }
    public string? AuthorizedSignatoryName { get; init; }
    public string? LegalStatusLabel { get; init; }
    public string? BillingConsolidationModeLabel { get; init; }
    public string? ParentCustomerDisplayName { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }
    public List<Customer360ContactDto> Contacts { get; init; } = new();
    public List<Customer360SubscriptionDto> ActiveSubscriptions { get; init; } = new();
    public List<Customer360OperationDto> RecentOperations { get; init; } = new();
}

public class GetCustomer360Request : IRequest<GetCustomer360Result>
{
    public string CustomerId { get; init; } = "";
}

public class GetCustomer360Handler : IRequestHandler<GetCustomer360Request, GetCustomer360Result>
{
    private readonly IQueryContext _query;
    private readonly ISubscriberAccessAuditService _subscriberAudit;

    public GetCustomer360Handler(IQueryContext query, ISubscriberAccessAuditService subscriberAudit)
    {
        _query = query;
        _subscriberAudit = subscriberAudit;
    }

    public async Task<GetCustomer360Result> Handle(GetCustomer360Request request, CancellationToken cancellationToken)
    {
        var customer = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .Include(c => c.CustomerGroup)
            .Include(c => c.CustomerCategory)
            .Include(c => c.CustomerContactList.Where(cc => !cc.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");

        string? nationalMasked = null;
        string? commercialRegistry = null;
        DateOnly? dateOfBirth = null;
        string? nationality = null;
        string? genderLabel = null;
        string? occupation = null;
        string? taxNumber = null;
        string? authorizedSignatory = null;
        string? legalStatusLabel = null;
        string? billingConsolidationLabel = null;
        string? parentCustomerName = null;

        if (customer is IndividualCustomer ind)
        {
            var decrypted = ind.NationalId;
            nationalMasked = decrypted.Length >= 4
                ? new string('*', decrypted.Length - 4) + decrypted[^4..]
                : "****";
            dateOfBirth = ind.DateOfBirth;
            nationality = ind.Nationality;
            genderLabel = ind.Gender switch
            {
                Gender.Male => "ذكر",
                Gender.Female => "أنثى",
                _ => "غير محدد",
            };
            occupation = ind.Occupation;
        }
        else if (customer is CorporateCustomer corp)
        {
            commercialRegistry = corp.CommercialRegistryNumber;
            taxNumber = corp.TaxNumber;
            authorizedSignatory = corp.AuthorizedSignatoryName;
            legalStatusLabel = corp.LegalStatus switch
            {
                CompanyLegalStatus.SoleProprietorship => "مؤسسة فردية",
                CompanyLegalStatus.Partnership => "شراكة",
                CompanyLegalStatus.LimitedLiability => "ذات مسؤولية محدودة",
                CompanyLegalStatus.JointStock => "مساهمة",
                CompanyLegalStatus.Government => "حكومية",
                _ => "غير محدد",
            };
            billingConsolidationLabel = corp.BillingConsolidationMode switch
            {
                BillingConsolidationMode.Unified => "موحّد",
                _ => "منفصل",
            };
            if (!string.IsNullOrEmpty(corp.ParentCustomerId))
            {
                parentCustomerName = await _query.Customer.AsNoTracking()
                    .Where(c => c.Id == corp.ParentCustomerId)
                    .Select(c => c.DisplayName)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        var profiles = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == customer.Id)
            .Include(p => p.SimInventories)
            .ToListAsync(cancellationToken);

        var profileIds = profiles.Select(p => p.Id).ToList();
        var profileById = profiles.ToDictionary(p => p.Id);

        var subscriptions = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => profileIds.Contains(s.SubscriberProfileId))
            .Include(s => s.MsisdnAsset)
            .Include(s => s.Product)
            .Include(s => s.ProductOffering!)
                .ThenInclude(o => o.Components)
            .Include(s => s.SubscriptionTypeLookup)
            .OrderByDescending(s => s.IsPrimaryLine)
            .ThenBy(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var productIds = subscriptions
            .Select(s => s.ProductId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var offeringIds = subscriptions
            .Select(s => s.ProductOfferingId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var offeringsByProductId = productIds.Count == 0
            ? new Dictionary<string, ProductOffering>()
            : await _query.ProductOffering.AsNoTracking()
                .Where(o => !o.IsDeleted && o.ProductId != null && productIds.Contains(o.ProductId))
                .Include(o => o.Components)
                .GroupBy(o => o.ProductId!)
                .ToDictionaryAsync(g => g.Key, g => g.OrderBy(x => x.SortOrder).First(), cancellationToken);

        var offeringsById = offeringIds.Count == 0
            ? new Dictionary<string, ProductOffering>()
            : await _query.ProductOffering.AsNoTracking()
                .Where(o => !o.IsDeleted && offeringIds.Contains(o.Id))
                .Include(o => o.Components)
                .ToDictionaryAsync(o => o.Id, cancellationToken);

        var msisdnAssetIds = subscriptions
            .Select(s => s.MsisdnAssetId)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();

        var operationSimByAsset = msisdnAssetIds.Count == 0
            ? new Dictionary<string, string>()
            : await (
                from op in _query.TelecomOperationRequest.AsNoTracking()
                where op.MsisdnAssetId != null
                      && op.SimInventoryId != null
                      && msisdnAssetIds.Contains(op.MsisdnAssetId)
                orderby op.CreatedAtUtc descending
                select new { op.MsisdnAssetId, op.SimInventoryId })
                .GroupBy(x => x.MsisdnAssetId!)
                .ToDictionaryAsync(g => g.Key, g => g.First().SimInventoryId!, cancellationToken);

        var operationSimIds = operationSimByAsset.Values.Distinct().ToList();
        var operationSims = operationSimIds.Count == 0
            ? new Dictionary<string, SimInventory>()
            : await _query.SimInventory.AsNoTracking()
                .Where(s => operationSimIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);

        var iccidCandidates = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sub in subscriptions)
        {
            var asset = sub.MsisdnAsset;
            if (asset == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(asset.PairedIccid))
            {
                iccidCandidates.Add(asset.PairedIccid.Trim());
            }

            var derived = MsisdnAssetKitResolver.DeriveIccidFromMsisdn(asset.Msisdn);
            if (!string.IsNullOrWhiteSpace(derived))
            {
                iccidCandidates.Add(derived);
            }
        }

        var simsByIccid = iccidCandidates.Count == 0
            ? new Dictionary<string, SimInventory>(StringComparer.Ordinal)
            : await _query.SimInventory.AsNoTracking()
                .Where(s => iccidCandidates.Contains(s.Iccid))
                .ToDictionaryAsync(s => s.Iccid, StringComparer.Ordinal, cancellationToken);

        var subscriptionDtos = subscriptions.Select(s =>
        {
            ProductOffering? offering = null;
            if (!string.IsNullOrEmpty(s.ProductOfferingId))
            {
                if (s.ProductOffering != null && !s.ProductOffering.IsDeleted)
                {
                    offering = s.ProductOffering;
                }
                else if (offeringsById.TryGetValue(s.ProductOfferingId, out var direct))
                {
                    offering = direct;
                }
            }

            if (offering == null && !string.IsNullOrEmpty(s.ProductId))
            {
                offeringsByProductId.TryGetValue(s.ProductId, out offering);
            }

            profileById.TryGetValue(s.SubscriberProfileId, out var profile);
            var asset = s.MsisdnAsset;

            SimInventory? operationSim = null;
            if (asset != null
                && !string.IsNullOrEmpty(s.MsisdnAssetId)
                && operationSimByAsset.TryGetValue(s.MsisdnAssetId, out var opSimId)
                && operationSims.TryGetValue(opSimId, out var opSim))
            {
                operationSim = opSim;
            }

            var profileSim = MsisdnAssetKitResolver.ResolveLinkedSim(profile?.SimInventories);
            string? iccid = null;
            string? imsi = null;
            SimInventory? resolvedSim = operationSim ?? profileSim;

            if (asset != null)
            {
                (iccid, imsi) = MsisdnAssetKitResolver.Resolve(asset, profileSim, operationSim, simsByIccid);
                if (resolvedSim == null
                    && !string.IsNullOrWhiteSpace(iccid)
                    && simsByIccid.TryGetValue(iccid, out var simByIccid))
                {
                    resolvedSim = simByIccid;
                }
            }

            var defaultSimStatus = MsisdnAssetKitResolver.DefaultSimStatusForProfile(profile?.OperationalStatus);
            var simTypeLabel = MsisdnAssetKitResolver.SimTypeLabel(resolvedSim?.SimType ?? SimType.Physical);
            var simStatusLabel = resolvedSim != null
                ? MsisdnAssetKitResolver.SimStatusLabel(resolvedSim.Status)
                : defaultSimStatus.HasValue
                    ? MsisdnAssetKitResolver.SimStatusLabel(defaultSimStatus)
                    : "—";

            var packageComponents = (offering?.Components ?? [])
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.SortOrder)
                .Select(c => new Customer360PackageComponentDto(
                    c.ComponentType,
                    c.Label,
                    c.Quota,
                    c.QuotaUnit,
                    c.IsUnlimited,
                    c.SortOrder))
                .ToList();

            return new Customer360SubscriptionDto(
                s.Id,
                s.SubscriberProfileId,
                asset?.Msisdn,
                s.MsisdnAssetId,
                s.IsPrimaryLine,
                s.ProductId,
                s.Product?.Name,
                offering?.Id,
                offering?.Name ?? offering?.NameEn,
                s.SubscriptionTypeLookup?.NameAr ?? s.SubscriptionTypeLookup?.Code,
                s.SubscriptionTypeLookup?.Code,
                simTypeLabel,
                iccid,
                profile?.OperationalStatus.ToString(),
                s.DocumentStatus switch
                {
                    TelecomDocumentStatus.Uploaded => "مرفوع",
                    TelecomDocumentStatus.Verified => "موثّق",
                    TelecomDocumentStatus.Rejected => "مرفوض",
                    _ => "ناقص",
                },
                profile?.ServiceLineType switch
                {
                    ServiceLineType.Broadband => "إنترنت",
                    ServiceLineType.FixedLine => "خط ثابت",
                    _ => "موبايل",
                },
                profile?.LanguagePreference switch
                {
                    LanguagePreference.English => "English",
                    _ => "العربية",
                },
                profile?.ActivationDateUtc,
                profile?.LoyaltyPoints ?? 0,
                profile?.LoyaltyTier,
                profile?.PrepaidBalance,
                profile?.PostpaidCreditLimit,
                profile?.ChurnRiskScore,
                simStatusLabel,
                imsi,
                s.CreatedAtUtc,
                packageComponents);
        }).ToList();

        var operationRows = await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
            .Where(o => profileIds.Contains(o.SubscriberProfileId)
                        || (o.SecondarySubscriberProfileId != null
                            && profileIds.Contains(o.SecondarySubscriberProfileId)))
            .Include(o => o.MsisdnAsset)
            .Include(o => o.SubscriberProfile!).ThenInclude(p => p!.Customer)
            .Include(o => o.SecondarySubscriberProfile!).ThenInclude(p => p!.Customer)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Take(15)
            .ToListAsync(cancellationToken);

        var operations = operationRows.Select(o =>
        {
            var isIncomingTakeOver = o.Kind == TelecomOperationKind.TakeOver
                && o.SecondarySubscriberProfileId != null
                && profileIds.Contains(o.SecondarySubscriberProfileId);
            var counterparty = isIncomingTakeOver
                ? o.SubscriberProfile?.Customer?.DisplayName
                : o.SecondarySubscriberProfile?.Customer?.DisplayName;

            return new Customer360OperationDto(
                o.Id,
                o.Number,
                o.Kind,
                TelecomOperationLabels.KindLabelAr(o.Kind),
                o.Status,
                TelecomOperationLabels.StatusLabelAr(o.Status),
                o.TransferReason,
                o.MsisdnAsset?.Msisdn,
                counterparty,
                o.CorrelationId,
                o.CreatedAtUtc);
        }).ToList();

        var primaryMsisdn = subscriptionDtos
            .OrderByDescending(s => s.IsPrimaryLine)
            .Select(s => s.Msisdn)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

        await _subscriberAudit.LogProfileViewAsync(
            customer.Id,
            customer.DisplayName,
            primaryMsisdn ?? customer.PrimaryPhone,
            cancellationToken);

        var contacts = customer.CustomerContactList
            .Where(c => !c.IsDeleted)
            .Select(c => new Customer360ContactDto(
                c.Id,
                c.Name,
                c.JobTitle,
                c.PhoneNumber,
                c.EmailAddress,
                c.Description))
            .ToList();

        return new GetCustomer360Result
        {
            CustomerId = customer.Id,
            DisplayName = customer.DisplayName,
            AccountNumber = customer.AccountNumber,
            Description = customer.Description,
            CustomerKind = customer.CustomerKind,
            CustomerKindLabel = customer.CustomerKind switch
            {
                CustomerKind.Corporate => "شركة",
                CustomerKind.Individual => "فرد",
                _ => customer.CustomerKind.ToString(),
            },
            Status = customer.Status,
            StatusLabel = customer.Status switch
            {
                CustomerStatus.Active => "نشط",
                CustomerStatus.Suspended => "موقوف",
                CustomerStatus.Closed => "مغلق",
                CustomerStatus.Blacklisted => "قائمة سوداء",
                _ => customer.Status.ToString(),
            },
            StatusReasonCodeLabel = customer.StatusReasonCode switch
            {
                CustomerStatusReasonCode.Credit => "ائتمان",
                CustomerStatusReasonCode.Fraud => "احتيال",
                CustomerStatusReasonCode.Regulatory => "تنظيمي",
                _ => null,
            },
            StatusReasonNote = customer.StatusReasonNote,
            NationalIdMasked = nationalMasked,
            CommercialRegistry = commercialRegistry,
            PrimaryPhone = customer.PrimaryPhone,
            ContactEmail = customer.ContactEmail,
            FaxNumber = customer.FaxNumber,
            Website = customer.Website,
            WhatsApp = customer.WhatsApp,
            LinkedIn = customer.LinkedIn,
            Facebook = customer.Facebook,
            Instagram = customer.Instagram,
            TwitterX = customer.TwitterX,
            TikTok = customer.TikTok,
            Street = customer.Address.Street,
            City = customer.Address.City,
            State = customer.Address.State,
            ZipCode = customer.Address.ZipCode,
            Country = customer.Address.Country,
            CustomerGroupName = customer.CustomerGroup?.Name,
            CustomerCategoryName = customer.CustomerCategory?.Name,
            DateOfBirth = dateOfBirth,
            Nationality = nationality,
            GenderLabel = genderLabel,
            Occupation = occupation,
            TaxNumber = taxNumber,
            AuthorizedSignatoryName = authorizedSignatory,
            LegalStatusLabel = legalStatusLabel,
            BillingConsolidationModeLabel = billingConsolidationLabel,
            ParentCustomerDisplayName = parentCustomerName,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc,
            Contacts = contacts,
            ActiveSubscriptions = subscriptionDtos,
            RecentOperations = operations
        };
    }
}
