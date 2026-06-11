using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public sealed class ForceHlrSyncResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string? TicketNumber { get; init; }
    public string? Msisdn { get; init; }
    public bool TicketAutoResolved { get; init; }
}

public class ForceHlrSyncByTicketRequest : IRequest<ForceHlrSyncResult>, IRequirePermission
{
    public string TicketId { get; init; } = "";
    public string? ActorUserId { get; init; }
    public string? IpAddress { get; init; }
    public string PermissionKey => PermissionCatalog.NetworkTechnicalSync;
}

public class ForceHlrSyncByTicketValidator : AbstractValidator<ForceHlrSyncByTicketRequest>
{
    public ForceHlrSyncByTicketValidator() => RuleFor(x => x.TicketId).NotEmpty();
}

public class ForceHlrSyncByTicketHandler : IRequestHandler<ForceHlrSyncByTicketRequest, ForceHlrSyncResult>
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHLRLiveStatusService _hlr;
    private readonly IUserAuditService _audit;
    private readonly ISmsGatewayIntegration _sms;

    public ForceHlrSyncByTicketHandler(
        IQueryContext query,
        ICommandRepository<TelecomTechnicalTicket> ticketRepository,
        IUnitOfWork unitOfWork,
        IHLRLiveStatusService hlr,
        IUserAuditService audit,
        ISmsGatewayIntegration sms)
    {
        _query = query;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _hlr = hlr;
        _audit = audit;
        _sms = sms;
    }

    public async Task<ForceHlrSyncResult> Handle(ForceHlrSyncByTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .Where(t => t.Id == request.TicketId)
            .Select(t => new { t.Id, t.TicketNumber, t.Msisdn, t.IssueType, t.SubscriberProfileId, t.CustomerId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("التذكرة غير موجودة.");

        var line = await TechnicalTicketLineResolver.ResolveAsync(
            _query,
            ticket.SubscriberProfileId,
            ticket.Msisdn,
            cancellationToken);

        // HARD TECHNICAL PROVISIONING COMMAND: DELETE / Purge cache followed by a fresh Unbar
        // We simulate this via ReprovisionSubscriberAsync which handles the "hard" logic in the integration layer
        var reprovision = await _hlr.ReprovisionSubscriberAsync(
            new HlrReprovisionRequest(
                line.SubscriberProfileId,
                line.Msisdn,
                null, // IMSI handled by service
                null, // ICCID handled by service
                request.ActorUserId),
            cancellationToken);

        if (!reprovision.Success)
        {
            throw new InvalidOperationException(reprovision.Message);
        }

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.ActorUserId ?? "",
                ActionType = UserAuditActionTypes.NetworkCommandExecuted,
                EntityType = "Huawei_HLR",
                EntityId = ticket.Id,
                SummaryAr = $"إعادة تهيئة فنية صلبة (Hard Reprovision) للخط {line.Msisdn} (تذكرة {ticket.TicketNumber})",
                Payload = new
                {
                    ticket.TicketNumber,
                    msisdn = line.Msisdn,
                    reprovision.Message,
                    reprovision.HlrSubscriberState
                },
                IpAddress = request.IpAddress,
            },
            cancellationToken);

        // Auto-resolve ticket
        var ticketEntity = await _ticketRepository.GetAsync(ticket.Id, cancellationToken);
        if (ticketEntity != null && ticketEntity.Status != TechnicalTicketStatus.Resolved)
        {
            ticketEntity.Status = TechnicalTicketStatus.Resolved;
            ticketEntity.ResolutionNotes = $"تمت المزامنة الفنية الصلبة (HLR Reprovision) بنجاح — {reprovision.Message}";
            ticketEntity.ResolvedByUserId = request.ActorUserId;
            ticketEntity.ResolvedAtUtc = DateTime.UtcNow;
            ticketEntity.UpdatedById = request.ActorUserId;
            _ticketRepository.Update(ticketEntity);
            await _unitOfWork.SaveAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(line.Msisdn))
            {
                var smsBody = $"سيريتل: تم حل بلاغكم الفني رقم {ticket.TicketNumber} وإعادة مزامنة الشبكة. شكراً لتواصلكم معنا.";
                await _sms.SendAsync(line.Msisdn, smsBody, cancellationToken);
            }
        }

        return new ForceHlrSyncResult
        {
            Success = true,
            Message = reprovision.Message,
            TicketNumber = ticket.TicketNumber,
            Msisdn = line.Msisdn,
            TicketAutoResolved = true
        };
    }
}
