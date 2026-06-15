using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Queries;

public class ResolveAuditDisplayNamesResult
{
    public string? ProfileDisplay { get; init; }
    public string? TicketNumber { get; init; }
    public string? CustomerDisplayName { get; init; }
}

public class ResolveAuditDisplayNamesRequest : IRequest<ResolveAuditDisplayNamesResult>, IRequireAnyPermission
{
    public string? ProfileId { get; init; }
    public string? TechnicalTicketId { get; init; }
    public string? CustomerId { get; init; }
    public IReadOnlyList<string> PermissionKeys => BackOfficePermissionSets.OperationsAny;
}

public class ResolveAuditDisplayNamesHandler : IRequestHandler<ResolveAuditDisplayNamesRequest, ResolveAuditDisplayNamesResult>
{
    private readonly IQueryContext _query;

    public ResolveAuditDisplayNamesHandler(IQueryContext query) => _query = query;

    public async Task<ResolveAuditDisplayNamesResult> Handle(
        ResolveAuditDisplayNamesRequest request,
        CancellationToken cancellationToken)
    {
        string? profileDisplay = null;
        string? customerName = null;
        string? ticketNumber = null;

        var profileId = (request.ProfileId ?? "").Trim();
        if (!string.IsNullOrEmpty(profileId))
        {
            var profile = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                .Include(p => p.Customer)
                .Include(p => p.Subscriptions)
                    .ThenInclude(s => s.MsisdnAsset)
                .FirstOrDefaultAsync(p => p.Id == profileId, cancellationToken);

            if (profile != null)
            {
                customerName = profile.Customer?.DisplayName;
                var msisdn = profile.Subscriptions
                    .Where(s => !s.IsDeleted && s.MsisdnAsset != null)
                    .OrderByDescending(s => s.IsPrimaryLine)
                    .Select(s => s.MsisdnAsset!.Msisdn)
                    .FirstOrDefault();
                profileDisplay = FormatProfileLabel(customerName, msisdn);
            }
        }

        var customerId = (request.CustomerId ?? "").Trim();
        if (string.IsNullOrEmpty(customerName) && !string.IsNullOrEmpty(customerId))
        {
            customerName = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
                .Where(c => c.Id == customerId)
                .Select(c => c.DisplayName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var ticketId = (request.TechnicalTicketId ?? "").Trim();
        if (!string.IsNullOrEmpty(ticketId))
        {
            ticketNumber = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
                .Where(t => t.Id == ticketId)
                .Select(t => t.TicketNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ResolveAuditDisplayNamesResult
        {
            ProfileDisplay = profileDisplay,
            TicketNumber = ticketNumber,
            CustomerDisplayName = customerName,
        };
    }

    private static string? FormatProfileLabel(string? customerName, string? msisdn)
    {
        var name = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        var phone = string.IsNullOrWhiteSpace(msisdn) ? null : msisdn.Trim();
        if (name != null && phone != null) return $"{name} — {phone}";
        return name ?? phone;
    }
}
