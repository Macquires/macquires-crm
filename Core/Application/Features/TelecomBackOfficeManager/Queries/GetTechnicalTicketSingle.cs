using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Security;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Queries;

public record GetTechnicalTicketSingleDto(
    string Id,
    string TicketNumber,
    string Msisdn,
    string? CustomerId,
    string? SubscriberProfileId,
    TechnicalTicketIssueType IssueType,
    TechnicalTicketCategory TicketCategory,
    TechnicalTicketPriority Priority,
    TechnicalTicketStatus Status,
    string? Notes,
    string? ResolutionNotes,
    string? PayloadJson,
    string OpenedByUserId,
    string? ResolvedByUserId,
    DateTime? CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    string CreatedByChannel);

public class GetTechnicalTicketSingleResult
{
    public GetTechnicalTicketSingleDto? Data { get; init; }
}

public class GetTechnicalTicketSingleRequest : IRequest<GetTechnicalTicketSingleResult>, IRequireAnyPermission
{
    public string Id { get; init; } = "";
    public IReadOnlyList<string> PermissionKeys => BackOfficePermissionSets.TechnicalTicketListAny;
}

public class GetTechnicalTicketSingleHandler : IRequestHandler<GetTechnicalTicketSingleRequest, GetTechnicalTicketSingleResult>
{
    private readonly IQueryContext _query;

    public GetTechnicalTicketSingleHandler(IQueryContext query) => _query = query;

    public async Task<GetTechnicalTicketSingleResult> Handle(GetTechnicalTicketSingleRequest request, CancellationToken cancellationToken)
    {
        var row = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .Where(t => t.Id == request.Id)
            .Select(t => new GetTechnicalTicketSingleDto(
                t.Id,
                t.TicketNumber,
                t.Msisdn,
                t.CustomerId,
                t.SubscriberProfileId,
                t.IssueType,
                t.TicketCategory,
                t.Priority,
                t.Status,
                t.Notes,
                t.ResolutionNotes,
                t.PayloadJson,
                t.OpenedByUserId,
                t.ResolvedByUserId,
                t.CreatedAtUtc,
                t.ResolvedAtUtc,
                t.CreatedByChannel))
            .FirstOrDefaultAsync(cancellationToken);

        return new GetTechnicalTicketSingleResult { Data = row };
    }
}
