using Application.Common.Audit;
using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public sealed class EscalateTechnicalTicketToTier3Result
{
    public string TicketId { get; init; } = "";
    public string TicketNumber { get; init; } = "";
    public TechnicalTicketStatus Status { get; init; }
}

public class EscalateTechnicalTicketToTier3Request : IRequest<EscalateTechnicalTicketToTier3Result>, IRequirePermission
{
    public string TicketId { get; init; } = "";
    public string EscalationNotes { get; init; } = "";
    public string? ActorUserId { get; init; }
    public string? IpAddress { get; init; }
    public string PermissionKey => PermissionCatalog.TelecomTicketEscalate;
}

public class EscalateTechnicalTicketToTier3Validator : AbstractValidator<EscalateTechnicalTicketToTier3Request>
{
    public EscalateTechnicalTicketToTier3Validator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.EscalationNotes).NotEmpty().MinimumLength(10)
            .WithMessage("أدخل سبب التصعيد (10 أحرف على الأقل).");
        RuleFor(x => x.ActorUserId).NotEmpty();
    }
}

public class EscalateTechnicalTicketToTier3Handler
    : IRequestHandler<EscalateTechnicalTicketToTier3Request, EscalateTechnicalTicketToTier3Result>
{
    private readonly ICommandRepository<TelecomTechnicalTicket> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAuditService _audit;

    public EscalateTechnicalTicketToTier3Handler(
        ICommandRepository<TelecomTechnicalTicket> repository,
        IUnitOfWork unitOfWork,
        IUserAuditService audit)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _audit = audit;
    }

    public async Task<EscalateTechnicalTicketToTier3Result> Handle(
        EscalateTechnicalTicketToTier3Request request,
        CancellationToken cancellationToken)
    {
        var ticket = await _repository.GetAsync(request.TicketId, cancellationToken)
            ?? throw new InvalidOperationException("التذكرة غير موجودة.");

        if (ticket.Status == TechnicalTicketStatus.Resolved)
        {
            throw new InvalidOperationException("لا يمكن تصعيد تذكرة مغلقة.");
        }

        var notes = request.EscalationNotes.Trim();
        ticket.Status = TechnicalTicketStatus.Escalated;
        ticket.Priority = TechnicalTicketPriority.Critical;
        ticket.ResolutionNotes = string.IsNullOrWhiteSpace(ticket.ResolutionNotes)
            ? $"[Tier-3] {notes}"
            : $"{ticket.ResolutionNotes}\n[Tier-3] {notes}";
        ticket.UpdatedById = request.ActorUserId;

        _repository.Update(ticket);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.ActorUserId!,
                ActionType = UserAuditActionTypes.TelecomOperationConfirmed,
                EntityType = nameof(TelecomTechnicalTicket),
                EntityId = ticket.Id,
                SummaryAr = $"تصعيد تذكرة {ticket.TicketNumber} للقسم الهندسي (Tier-3): {notes}",
                Payload = new
                {
                    ticket.TicketNumber,
                    ticket.Msisdn,
                    escalationNotes = notes,
                    routedTo = "Tier-3-CoreNetwork",
                },
                IpAddress = request.IpAddress,
            },
            cancellationToken);

        return new EscalateTechnicalTicketToTier3Result
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            Status = ticket.Status,
        };
    }
}
