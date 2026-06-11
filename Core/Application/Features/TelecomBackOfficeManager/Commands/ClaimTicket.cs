using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom.BackOffice;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public sealed class ClaimTicketResult
{
    public string? Id { get; init; }
    public TelecomOperationStatus Status { get; init; }
    public string? AssignedAgentEmail { get; init; }
    public DateTime? ClaimedAt { get; init; }
    public string? PipelineState { get; init; }
}

public sealed class ClaimTicketRequest : IRequest<ClaimTicketResult>
{
    public string TicketId { get; init; } = null!;
    public string AgentEmail { get; init; } = null!;
    public string? ActorUserId { get; init; }
}

public sealed class ClaimTicketValidator : AbstractValidator<ClaimTicketRequest>
{
    public ClaimTicketValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.AgentEmail).NotEmpty().EmailAddress();
    }
}

public sealed class ClaimTicketHandler : IRequestHandler<ClaimTicketRequest, ClaimTicketResult>
{
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAuditService _audit;
    private readonly IPublisher _publisher;

    public ClaimTicketHandler(
        IQueryContext query,
        IUnitOfWork unitOfWork,
        IUserAuditService audit,
        IPublisher publisher)
    {
        _query = query;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _publisher = publisher;
    }

    public async Task<ClaimTicketResult> Handle(ClaimTicketRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Fetch with tracking to allow updates
            var ticket = await _query.TelecomOperationRequest
                .FirstOrDefaultAsync(o => o.Id == request.TicketId && !o.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Ticket not found.");

            // Strict Concurrency Check
            if (!string.IsNullOrEmpty(ticket.AssignedAgentEmail))
            {
                throw new TicketAlreadyClaimedException(
                    "عذراً، هذا الطلب محجوز مسبقاً من قبل موظف آخر.",
                    "This ticket has already been claimed by another agent.");
            }

            // Update state
            ticket.AssignedAgentEmail = request.AgentEmail;
            ticket.ClaimedByUserId = request.ActorUserId;
            ticket.Status = TelecomOperationStatus.In_Progress;
            ticket.ClaimedAt = DateTime.UtcNow;
            ticket.UpdatedById = request.ActorUserId;

            // Audit Log
            await _audit.LogAsync(new UserAuditLogRequest
            {
                ActorUserId = request.ActorUserId ?? "system",
                ActionType = UserAuditActionTypes.BackOfficeTicketClaimed,
                EntityType = nameof(TelecomOperationRequest),
                EntityId = ticket.Id,
                SummaryAr = $"تم حجز الطلب {ticket.Number} من قبل الموظف {request.AgentEmail}",
                Payload = new { ticket.Id, ticket.Number, request.AgentEmail, ClaimedAt = ticket.ClaimedAt }
            }, cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);

            return new ClaimTicketResult
            {
                Id = ticket.Id,
                Status = ticket.Status,
                AssignedAgentEmail = ticket.AssignedAgentEmail,
                ClaimedAt = ticket.ClaimedAt,
                PipelineState = BackOfficeTelecomPipelineState.Resolve(ticket.Status, ticket.ApprovalLevelRequired)
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleViolationException(
                "عذراً، حدث تعارض أثناء محاولة حجز الطلب. يبدو أن موظفاً آخر قام بحجزه في نفس اللحظة.",
                "Concurrency Conflict: This ticket was claimed by another agent simultaneously.");
        }
    }
}
