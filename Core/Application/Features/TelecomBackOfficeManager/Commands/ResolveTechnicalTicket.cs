using Application.Common.Audit;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public class ResolveTechnicalTicketResult
{
    public TelecomTechnicalTicket? Data { get; init; }
}

public class ResolveTechnicalTicketRequest : IRequest<ResolveTechnicalTicketResult>
{
    public string Id { get; init; } = "";
    public string ResolutionNotes { get; init; } = "";
    public string? ResolvedByUserId { get; init; }
    public string? IpAddress { get; init; }
}

public class ResolveTechnicalTicketValidator : AbstractValidator<ResolveTechnicalTicketRequest>
{
    public ResolveTechnicalTicketValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ResolutionNotes).NotEmpty().MinimumLength(5);
        RuleFor(x => x.ResolvedByUserId).NotEmpty();
    }
}

public class ResolveTechnicalTicketHandler : IRequestHandler<ResolveTechnicalTicketRequest, ResolveTechnicalTicketResult>
{
    private readonly ICommandRepository<TelecomTechnicalTicket> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAuditService _audit;
    private readonly ISmsGatewayIntegration _sms;

    public ResolveTechnicalTicketHandler(
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

    public async Task<ResolveTechnicalTicketResult> Handle(ResolveTechnicalTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _repository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Ticket not found.");

        ticket.Status = TechnicalTicketStatus.Resolved;
        ticket.ResolutionNotes = request.ResolutionNotes;
        ticket.ResolvedByUserId = request.ResolvedByUserId;
        ticket.ResolvedAtUtc = DateTime.UtcNow;
        ticket.UpdatedById = request.ResolvedByUserId;

        _repository.Update(ticket);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.ResolvedByUserId!,
                ActionType = UserAuditActionTypes.TicketResolved,
                EntityType = nameof(TelecomTechnicalTicket),
                EntityId = ticket.Id,
                SummaryAr = $"إغلاق التذكرة {ticket.TicketNumber} — {request.ResolutionNotes}",
                Payload = new { ticket.TicketNumber, ticket.Msisdn, request.ResolutionNotes },
                IpAddress = request.IpAddress,
            },
            cancellationToken);

        var smsBody = $"سيريتل: تم حل بلاغكم الفني رقم {ticket.TicketNumber}. شكراً لتواصلكم معنا.";
        await _sms.SendAsync(ticket.Msisdn, smsBody, cancellationToken);

        return new ResolveTechnicalTicketResult { Data = ticket };
    }
}
