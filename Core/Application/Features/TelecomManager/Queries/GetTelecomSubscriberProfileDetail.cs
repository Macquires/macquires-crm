using Application.Common.CQS.Queries;
using Application.Common.Security;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record TelecomSubscriptionLineDto
{
    public string Id { get; init; } = "";
    public string? Msisdn { get; init; }
    public string? Iccid { get; init; }
    public string SubscriptionTypeId { get; init; } = "";
    public string? SubscriptionTypeNameAr { get; init; }
    public string? SubscriptionTypeNameEn { get; init; }
    public string SubscriptionType { get; init; } = "";
    public string? SubscriptionTypeCode { get; init; }
    public bool IsPrimaryLine { get; init; }
}

public class GetTelecomSubscriberProfileDetailResult
{
    public string SubscriberProfileId { get; init; } = "";
    public string CustomerId { get; init; } = "";
    public string? CustomerName { get; init; }
    public string? CustomerNumber { get; init; }
    public string? SubscriberType { get; init; }
    public string? NationalId { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? CommercialRegistration { get; init; }
    public string? TaxNumber { get; init; }
    public string? AuthorizedSignatory { get; init; }
    public int LoyaltyPoints { get; init; }
    public string? LoyaltyTier { get; init; }
    public decimal? PrepaidBalance { get; init; }
    public decimal? PostpaidCreditLimit { get; init; }
    public int? ChurnRiskScore { get; init; }
    public string? PhoneNumber { get; init; }
    public string? EmailAddress { get; init; }
    public string? Street { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? ZipCode { get; init; }
    public string? Country { get; init; }
    public string? Description { get; init; }
    public string? OperationalStatus { get; init; }
    public List<TelecomSubscriptionLineDto> Subscriptions { get; init; } = new();
}

public class GetTelecomSubscriberProfileDetailRequest : IRequest<GetTelecomSubscriberProfileDetailResult?>, IRequireAnyPermission
{
    public string SubscriberProfileId { get; init; } = "";
    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.OperationsViewAny;
}

public class GetTelecomSubscriberProfileDetailHandler : IRequestHandler<GetTelecomSubscriberProfileDetailRequest, GetTelecomSubscriberProfileDetailResult?>
{
    private readonly IQueryContext _context;

    public GetTelecomSubscriberProfileDetailHandler(IQueryContext context) => _context = context;

    public async Task<GetTelecomSubscriberProfileDetailResult?> Handle(
        GetTelecomSubscriberProfileDetailRequest request,
        CancellationToken cancellationToken)
    {
        var id = request.SubscriberProfileId.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var profile = await _context.SubscriberProfile
            .AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.SimInventories)
            .Include(p => p.Subscriptions).ThenInclude(s => s.MsisdnAsset)
            .Include(p => p.Subscriptions).ThenInclude(s => s.SubscriptionTypeLookup)
            .FirstOrDefaultAsync(p => !p.IsDeleted && p.Id == id, cancellationToken);

        if (profile?.Customer == null)
        {
            return null;
        }

        var c = profile.Customer;
        var profileIccid = profile.SimInventories.FirstOrDefault(s => !s.IsDeleted)?.Iccid;

        var lines = profile.Subscriptions
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.IsPrimaryLine)
            .Select(s => new TelecomSubscriptionLineDto
            {
                Id = s.Id,
                Msisdn = s.MsisdnAsset?.Msisdn,
                Iccid = profileIccid,
                SubscriptionTypeId = s.SubscriptionTypeId,
                SubscriptionTypeNameAr = s.SubscriptionTypeLookup?.NameAr,
                SubscriptionTypeNameEn = s.SubscriptionTypeLookup?.NameEn,
                SubscriptionType = s.SubscriptionTypeLookup?.NameEn ?? s.SubscriptionTypeLookup?.Code ?? "",
                SubscriptionTypeCode = s.SubscriptionTypeLookup?.Code,
                IsPrimaryLine = s.IsPrimaryLine,
            })
            .ToList();

        return new GetTelecomSubscriberProfileDetailResult
        {
            SubscriberProfileId = profile.Id,
            CustomerId = profile.CustomerId,
            CustomerName = c.DisplayName,
            CustomerNumber = c.AccountNumber,
            SubscriberType = c.CustomerKind.ToString(),
            NationalId = c is IndividualCustomer i ? i.NationalId : null,
            DateOfBirth = c is IndividualCustomer ind && ind.DateOfBirth.HasValue
                ? ind.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : null,
            CommercialRegistration = c is CorporateCustomer corp ? corp.CommercialRegistryNumber : null,
            TaxNumber = c is CorporateCustomer corp2 ? corp2.TaxNumber : null,
            AuthorizedSignatory = c is CorporateCustomer corp3 ? corp3.AuthorizedSignatoryName : null,
            LoyaltyPoints = profile.LoyaltyPoints,
            LoyaltyTier = profile.LoyaltyTier,
            PrepaidBalance = profile.PrepaidBalance,
            PostpaidCreditLimit = profile.PostpaidCreditLimit,
            ChurnRiskScore = profile.ChurnRiskScore,
            PhoneNumber = c.PrimaryPhone,
            EmailAddress = c.ContactEmail,
            Street = c.Address.Street,
            City = c.Address.City,
            State = c.Address.State,
            ZipCode = c.Address.ZipCode,
            Country = c.Address.Country,
            Description = c.Description,
            OperationalStatus = profile.OperationalStatus.ToString(),
            Subscriptions = lines,
        };
    }
}
