using Application.Common.Audit;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public class UpdateTechnicalTicketStatusResult
{
    public TelecomTechnicalTicket? Data { get; init; }
}

public class UpdateTechnicalTicketStatusRequest : IRequest<UpdateTechnicalTicketStatusResult>
{
    public string TicketId { get; init; } = "";
    public TechnicalTicketStatus NewStatus { get; init; }
    public TechnicalTicketPriority? NewPriority { get; init; }
    public string? OperatorNotesAr { get; init; }
    public string? ActorUserId { get; init; }
    public string? IpAddress { get; init; }
}

public class UpdateTechnicalTicketStatusValidator : AbstractValidator<UpdateTechnicalTicketStatusRequest>
{
    public UpdateTechnicalTicketStatusValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.ActorUserId).NotEmpty();
        RuleFor(x => x.OperatorNotesAr)
            .NotEmpty()
            .MinimumLength(5)
            .When(x => x.NewStatus == TechnicalTicketStatus.Resolved)
            .WithMessage("أدخل ملاحظات الحل (5 أحرف على الأقل) عند الإغلاق.");
    }
}

public class UpdateTechnicalTicketStatusHandler
    : IRequestHandler<UpdateTechnicalTicketStatusRequest, UpdateTechnicalTicketStatusResult>
{
    private readonly ICommandRepository<TelecomTechnicalTicket> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAuditService _audit;
    private readonly ISmsGatewayIntegration _sms;

    public UpdateTechnicalTicketStatusHandler(
        ICommandRepository<TelecomTechnicalTicket> repository,
        IUnitOfWork unitOfWork,
        IUserAuditService audit,
        ISmsGatewayIntegration sms)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _sms = sms;
    }

    public async Task<UpdateTechnicalTicketStatusResult> Handle(
        UpdateTechnicalTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        var ticket = await _repository.GetAsync(request.TicketId, cancellationToken)
            ?? throw new InvalidOperationException("التذكرة غير موجودة.");

        var previousStatus = ticket.Status;
        var previousPriority = ticket.Priority;
        ticket.Status = request.NewStatus;
        if (request.NewPriority.HasValue)
        {
            ticket.Priority = request.NewPriority.Value;
        }
        ticket.UpdatedById = request.ActorUserId;

        if (request.NewStatus == TechnicalTicketStatus.Resolved)
        {
            ticket.ResolutionNotes = request.OperatorNotesAr?.Trim();
            ticket.ResolvedByUserId = request.ActorUserId;
            ticket.ResolvedAtUtc = DateTime.UtcNow;
        }
        else if (!string.IsNullOrWhiteSpace(request.OperatorNotesAr))
        {
            ticket.ResolutionNotes = request.OperatorNotesAr.Trim();
        }

        if (request.NewStatus is TechnicalTicketStatus.Open or TechnicalTicketStatus.InProgress)
        {
            ticket.ResolvedAtUtc = null;
            ticket.ResolvedByUserId = null;
        }

        _repository.Update(ticket);
        await _unitOfWork.SaveAsync(cancellationToken);

        var notes = request.OperatorNotesAr?.Trim() ?? "—";
        var priorityPart = request.NewPriority.HasValue && request.NewPriority != previousPriority
            ? $" الأولوية: {previousPriority} → {request.NewPriority}."
            : "";
        var summary =
            $"تحديث تذكرة {ticket.TicketNumber} الحالة: {previousStatus} → {request.NewStatus}.{priorityPart} الملاحظات: {notes}";

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.ActorUserId!,
                ActionType = request.NewStatus == TechnicalTicketStatus.Resolved
                    ? UserAuditActionTypes.TicketResolved
                    : UserAuditActionTypes.TelecomOperationConfirmed,
                EntityType = nameof(TelecomTechnicalTicket),
                EntityId = ticket.Id,
                SummaryAr = summary,
                Payload = new
                {
                    ticket.TicketNumber,
                    ticket.Msisdn,
                    previousStatus = previousStatus.ToString(),
                    newStatus = request.NewStatus.ToString(),
                    previousPriority = previousPriority.ToString(),
                    newPriority = ticket.Priority.ToString(),
                    operatorNotesAr = notes,
                },
                IpAddress = request.IpAddress,
            },
            cancellationToken);

        if (request.NewStatus == TechnicalTicketStatus.Resolved)
        {
            var smsBody = $"سيريتل: تم حل بلاغكم الفني رقم {ticket.TicketNumber}. شكراً لتواصلكم معنا.";
            await _sms.SendAsync(ticket.Msisdn, smsBody, cancellationToken);
        }

        return new UpdateTechnicalTicketStatusResult { Data = ticket };
    }
}
