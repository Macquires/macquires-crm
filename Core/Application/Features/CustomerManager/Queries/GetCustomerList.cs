using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public record GetCustomerTelecomLineDto
{
    public string? SubscriberProfileId { get; init; }
    public string? SubscriberProfileNationalId { get; init; }
    public string? SubscriberProfileLoyaltyTier { get; init; }
    public decimal? PrepaidBalance { get; init; }
    public decimal? PostpaidCreditLimit { get; init; }
    public string? TelecomSubscriptionId { get; init; }
    public string? Msisdn { get; init; }
    public bool IsPrimaryLine { get; init; }
    public string? SubscriptionTypeName { get; init; }
    public string? SubscriptionTypeCode { get; init; }
}

public record GetCustomerListDto
{
    public string? Id { get; init; }
    public string? Name { get; set; }
    public string? Number { get; set; }
    public string? Description { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? FaxNumber { get; set; }
    public string? EmailAddress { get; set; }
    public string? Website { get; set; }
    public string? WhatsApp { get; set; }
    public string? LinkedIn { get; set; }
    public string? Facebook { get; set; }
    public string? Instagram { get; set; }
    public string? TwitterX { get; set; }
    public string? TikTok { get; set; }
    public string? CustomerGroupId { get; set; }
    public string? CustomerGroupName { get; set; }
    public string? CustomerCategoryId { get; set; }
    public string? CustomerCategoryName { get; set; }
    public string? CreatedById { get; init; }
    public DateTime? CreatedAtUtc { get; init; }

    public string? SubscriberType { get; set; }
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? CommercialRegistration { get; set; }
    public string? TaxNumber { get; set; }
    public string? AuthorizedSignatory { get; set; }

    public string? PrimaryMsisdn { get; set; }
    public string? SubscriptionTypeId { get; set; }
    public string? SubscriptionTypeCode { get; set; }
    public string? SubscriptionTypeName { get; set; }
    public string? SubscriptionTypeDisplayColor { get; set; }
    public int TelecomSubscriptionLineCount { get; set; }
    public string? TelecomMsisdnsSummary { get; set; }
    public List<GetCustomerTelecomLineDto>? TelecomLines { get; set; }
}

public class GetCustomerListProfile : Profile
{
    public GetCustomerListProfile()
    {
        CreateMap<Customer, GetCustomerListDto>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.DisplayName))
            .ForMember(d => d.Number, o => o.MapFrom(s => s.AccountNumber))
            .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.PrimaryPhone))
            .ForMember(d => d.EmailAddress, o => o.MapFrom(s => s.ContactEmail))
            .ForMember(d => d.Street, o => o.MapFrom(s => s.Address.Street))
            .ForMember(d => d.City, o => o.MapFrom(s => s.Address.City))
            .ForMember(d => d.State, o => o.MapFrom(s => s.Address.State))
            .ForMember(d => d.ZipCode, o => o.MapFrom(s => s.Address.ZipCode))
            .ForMember(d => d.Country, o => o.MapFrom(s => s.Address.Country))
            .ForMember(d => d.CustomerGroupName, o => o.MapFrom(s => s.CustomerGroup != null ? s.CustomerGroup.Name : string.Empty))
            .ForMember(d => d.CustomerCategoryName, o => o.MapFrom(s => s.CustomerCategory != null ? s.CustomerCategory.Name : string.Empty))
            .ForMember(d => d.SubscriberType, o => o.MapFrom(s => s.CustomerKind.ToString()))
            .ForMember(d => d.NationalId, o => o.Ignore())
            .ForMember(d => d.DateOfBirth, o => o.Ignore())
            .ForMember(d => d.CommercialRegistration, o => o.Ignore())
            .ForMember(d => d.TaxNumber, o => o.Ignore())
            .ForMember(d => d.AuthorizedSignatory, o => o.Ignore())
            .ForMember(dest => dest.PrimaryMsisdn, opt => opt.MapFrom(src =>
                src.SubscriberProfiles.Where(sp => !sp.IsDeleted).SelectMany(sp => sp.Subscriptions)
                    .OrderByDescending(sub => sub.IsPrimaryLine)
                    .Select(sub => sub.MsisdnAsset != null ? sub.MsisdnAsset.Msisdn : null)
                    .FirstOrDefault()))
            .ForMember(dest => dest.TelecomSubscriptionLineCount, opt => opt.MapFrom(src => src.SubscriberProfiles
                .Where(sp => !sp.IsDeleted).SelectMany(sp => sp.Subscriptions)
                .Count(s => !s.IsDeleted && s.MsisdnAsset != null)));
    }
}

public class GetCustomerListResult
{
    public List<GetCustomerListDto>? Data { get; init; }
    public int TotalCount { get; init; }
}

public class GetCustomerListRequest : IRequest<GetCustomerListResult>, IRequirePermission
{
    public bool IsDeleted { get; init; }
    public string? CustomerId { get; init; }
    public int Skip { get; init; }
    public int Take { get; init; }
    public string PermissionKey => PermissionCatalog.CustomerView;
}

public class GetCustomerListHandler : IRequestHandler<GetCustomerListRequest, GetCustomerListResult>
{
    private readonly IMapper _mapper;
    private readonly IQueryContext _context;

    public GetCustomerListHandler(IMapper mapper, IQueryContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<GetCustomerListResult> Handle(GetCustomerListRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Customer> query = _context.Customer
            .AsNoTracking()
            .IsDeletedEqualTo(request.IsDeleted)
            .Include(x => x.CustomerGroup)
            .Include(x => x.CustomerCategory)
            .Include(x => x.SubscriberProfiles.Where(sp => !sp.IsDeleted))
                .ThenInclude(sp => sp.Subscriptions.Where(s => !s.IsDeleted))
                    .ThenInclude(sub => sub.MsisdnAsset)
            .Include(x => x.SubscriberProfiles.Where(sp => !sp.IsDeleted))
                .ThenInclude(sp => sp.Subscriptions.Where(s => !s.IsDeleted))
                    .ThenInclude(sub => sub.SubscriptionTypeLookup);

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
        {
            query = query.Where(x => x.Id == request.CustomerId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var queryable = query.OrderByDescending(x => x.CreatedAtUtc).Skip(request.Skip);
        if (request.Take > 0)
        {
            queryable = queryable.Take(request.Take);
        }

        var entities = await queryable.AsSplitQuery().ToListAsync(cancellationToken);
        var dtos = _mapper.Map<List<GetCustomerListDto>>(entities);

        for (var i = 0; i < entities.Count; i++)
        {
            var c = entities[i];
            if (c is IndividualCustomer ind)
            {
                dtos[i].NationalId = ind.NationalId;
                dtos[i].DateOfBirth = ind.DateOfBirth?.ToDateTime(TimeOnly.MinValue);
            }
            else if (c is CorporateCustomer corp)
            {
                dtos[i].CommercialRegistration = corp.CommercialRegistryNumber;
                dtos[i].TaxNumber = corp.TaxNumber;
                dtos[i].AuthorizedSignatory = corp.AuthorizedSignatoryName;
            }

            dtos[i].TelecomMsisdnsSummary = BuildMsisdnsSummary(c);
            dtos[i].TelecomLines = BuildTelecomLines(c, dtos[i].NationalId);
        }

        return new GetCustomerListResult { Data = dtos, TotalCount = totalCount };
    }

    private static string? BuildMsisdnsSummary(Customer customer)
    {
        var rest = customer.SubscriberProfiles
            .Where(sp => !sp.IsDeleted)
            .SelectMany(sp => sp.Subscriptions)
            .Where(s => !s.IsDeleted && s.MsisdnAsset != null)
            .Select(s => s.MsisdnAsset!.Msisdn)
            .Where(m => !string.IsNullOrEmpty(m))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(m => m, StringComparer.Ordinal)
            .ToList();

        return rest.Count == 0 ? null : string.Join(" · ", rest.Take(4)) + (rest.Count > 4 ? $" (+{rest.Count - 4})" : "");
    }

    private static List<GetCustomerTelecomLineDto> BuildTelecomLines(Customer customer, string? partyNationalId)
    {
        return customer.SubscriberProfiles
            .Where(sp => !sp.IsDeleted)
            .SelectMany(sp => sp.Subscriptions.Where(s => !s.IsDeleted && s.MsisdnAsset != null)
                .Select(s => new GetCustomerTelecomLineDto
                {
                    SubscriberProfileId = sp.Id,
                    SubscriberProfileNationalId = partyNationalId,
                    SubscriberProfileLoyaltyTier = sp.LoyaltyTier,
                    PrepaidBalance = sp.PrepaidBalance,
                    PostpaidCreditLimit = sp.PostpaidCreditLimit,
                    TelecomSubscriptionId = s.Id,
                    Msisdn = s.MsisdnAsset!.Msisdn,
                    IsPrimaryLine = s.IsPrimaryLine,
                    SubscriptionTypeName = s.SubscriptionTypeLookup?.NameAr,
                    SubscriptionTypeCode = s.SubscriptionTypeLookup?.Code
                }))
            .OrderByDescending(x => x.IsPrimaryLine)
            .ThenBy(x => x.Msisdn ?? "", StringComparer.Ordinal)
            .ToList();
    }
}
