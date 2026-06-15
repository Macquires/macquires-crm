using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Application.Common.Telecom.Customer360;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public class GetCustomer360Request : IRequest<GetCustomer360Result>, IRequirePermission
{
    public string CustomerId { get; init; } = "";
    public string PermissionKey => PermissionCatalog.CustomerView;
}

public class GetCustomer360Handler : IRequestHandler<GetCustomer360Request, GetCustomer360Result>
{
    private readonly IQueryContext _query;
    private readonly ISubscriberAccessAuditService _subscriberAudit;
    private readonly ICustomer360KindDetailsResolver _kindDetails;
    private readonly ICustomer360SubscriptionAssembler _subscriptions;
    private readonly ICustomer360OperationsLoader _operations;

    public GetCustomer360Handler(
        IQueryContext query,
        ISubscriberAccessAuditService subscriberAudit,
        ICustomer360KindDetailsResolver kindDetails,
        ICustomer360SubscriptionAssembler subscriptions,
        ICustomer360OperationsLoader operations)
    {
        _query = query;
        _subscriberAudit = subscriberAudit;
        _kindDetails = kindDetails;
        _subscriptions = subscriptions;
        _operations = operations;
    }

    public async Task<GetCustomer360Result> Handle(GetCustomer360Request request, CancellationToken cancellationToken)
    {
        var customer = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .Include(c => c.CustomerGroup)
            .Include(c => c.CustomerCategory)
            .Include(c => c.CustomerContactList.Where(cc => !cc.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");

        var kindDetails = await _kindDetails.ResolveAsync(customer, cancellationToken);

        var subscriptionDtos = await _subscriptions.AssembleAsync(
            customer.Id,
            customer.CreatedById,
            cancellationToken);

        var profileIds = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.CustomerId == customer.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var operations = await _operations.LoadRecentAsync(profileIds, cancellationToken);

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
            NationalIdMasked = kindDetails.NationalIdMasked,
            CommercialRegistry = kindDetails.CommercialRegistry,
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
            DateOfBirth = kindDetails.DateOfBirth,
            Nationality = kindDetails.Nationality,
            Gender = kindDetails.Gender,
            GenderLabel = kindDetails.GenderLabel,
            Occupation = kindDetails.Occupation,
            TaxNumber = kindDetails.TaxNumber,
            AuthorizedSignatoryName = kindDetails.AuthorizedSignatoryName,
            LegalStatus = kindDetails.LegalStatus,
            LegalStatusLabel = kindDetails.LegalStatusLabel,
            BillingConsolidationMode = kindDetails.BillingConsolidationMode,
            BillingConsolidationModeLabel = kindDetails.BillingConsolidationModeLabel,
            ParentCustomerDisplayName = kindDetails.ParentCustomerDisplayName,
            CreatedAtUtc = customer.CreatedAtUtc,
            UpdatedAtUtc = customer.UpdatedAtUtc,
            Contacts = contacts,
            ActiveSubscriptions = subscriptionDtos.ToList(),
            RecentOperations = operations.ToList(),
        };
    }
}
