using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public sealed class HlrResyncByMsisdnResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public bool TicketAutoResolved { get; init; }
    public bool TicketMovedToInProgress { get; init; }
}

public class HlrResyncByMsisdnRequest : IRequest<HlrResyncByMsisdnResult>, IRequirePermission
{
    public string Msisdn { get; init; } = "";
    public string? TechnicalTicketId { get; init; }
    public string? ActorUserId { get; init; }
    public string? IpAddress { get; init; }
    public string PermissionKey => PermissionCatalog.NetworkTechnicalSync;
}

public class HlrResyncByMsisdnValidator : AbstractValidator<HlrResyncByMsisdnRequest>
{
    public HlrResyncByMsisdnValidator() => RuleFor(x => x.Msisdn).NotEmpty();
}

public class HlrResyncByMsisdnHandler : IRequestHandler<HlrResyncByMsisdnRequest, HlrResyncByMsisdnResult>
{
    private readonly IQueryContext _query;
    private readonly IMediator _mediator;
    private readonly IUserAuditService _audit;
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISmsGatewayIntegration _sms;

    public HlrResyncByMsisdnHandler(
        IQueryContext query,
        IMediator mediator,
        IUserAuditService audit,
        ICommandRepository<TelecomTechnicalTicket> ticketRepository,
        IUnitOfWork unitOfWork,
        ISmsGatewayIntegration sms)
    {
        _query = query;
        _mediator = mediator;
        _audit = audit;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _sms = sms;
    }

    public async Task<HlrResyncByMsisdnResult> Handle(HlrResyncByMsisdnRequest request, CancellationToken cancellationToken)
    {
        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.Msisdn)
            ?? throw new InvalidOperationException("Invalid MSISDN.");

        var profileId = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == msisdn)
            .Select(s => s.SubscriberProfileId)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("No subscriber profile for MSISDN.");

        var customerName = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.Id == profileId)
            .Select(p => p.Customer != null ? p.Customer.DisplayName : null)
            .FirstOrDefaultAsync(cancellationToken);

        var profileDisplay = string.IsNullOrWhiteSpace(customerName)
            ? msisdn
            : $"{customerName.Trim()} — {msisdn}";

        string? ticketNumber = null;
        if (!string.IsNullOrWhiteSpace(request.TechnicalTicketId))
        {
            ticketNumber = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
                .Where(t => t.Id == request.TechnicalTicketId)
                .Select(t => t.TicketNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var msisdnAssetId = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.SubscriberProfileId == profileId && s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == msisdn)
            .OrderByDescending(s => s.IsPrimaryLine)
            .Select(s => s.MsisdnAssetId)
            .FirstOrDefaultAsync(cancellationToken);

        var result = await _mediator.Send(
            new ResyncSubscriberFromHlrRequest
            {
                SubscriberProfileId = profileId,
                MsisdnAssetId = msisdnAssetId,
                Msisdn = msisdn,
                ActorUserId = request.ActorUserId,
            },
            cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = request.ActorUserId ?? "",
                ActionType = UserAuditActionTypes.NetworkCommandExecuted,
                EntityType = "HLR",
                EntityId = profileId,
                SummaryAr = $"مزامنة HLR للخط {msisdn}"
                    + (ticketNumber != null ? $" (تذكرة {ticketNumber})" : ""),
                Payload = AuditLogPayloadFactory.NetworkCommand(
                    msisdn,
                    profileId,
                    profileDisplay,
                    request.TechnicalTicketId,
                    ticketNumber),
                IpAddress = request.IpAddress,
            },
            cancellationToken);

        var (autoResolved, movedToInProgress) = await ApplyPostHlrTicketWorkflowAsync(
            request.TechnicalTicketId,
            ticketNumber,
            msisdn,
            result.Message,
            request.ActorUserId,
            request.IpAddress,
            cancellationToken);

        return new HlrResyncByMsisdnResult
        {
            Success = result.Success,
            Message = result.Message,
            TicketAutoResolved = autoResolved,
            TicketMovedToInProgress = movedToInProgress,
        };
    }

    private async Task<(bool AutoResolved, bool MovedToInProgress)> ApplyPostHlrTicketWorkflowAsync(
        string? technicalTicketId,
        string? ticketNumber,
        string msisdn,
        string hlrMessage,
        string? actorUserId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(technicalTicketId))
        {
            return (false, false);
        }

        var ticket = await _ticketRepository.GetAsync(technicalTicketId, cancellationToken);
        if (ticket == null || ticket.Status == TechnicalTicketStatus.Resolved)
        {
            return (false, false);
        }

        var autoResolve = ticket.TicketCategory is TechnicalTicketCategory.SimSwap or TechnicalTicketCategory.LineActivation
            || ticket.IssueType is TechnicalTicketIssueType.Network or TechnicalTicketIssueType.SimBlock;
        if (autoResolve)
        {
            var resolutionNotes = $"تمت مزامنة HLR — {hlrMessage}";
            ticket.Status = TechnicalTicketStatus.Resolved;
            ticket.ResolutionNotes = resolutionNotes;
            ticket.ResolvedByUserId = actorUserId;
            ticket.ResolvedAtUtc = DateTime.UtcNow;
            ticket.UpdatedById = actorUserId;
            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveAsync(cancellationToken);

            await _audit.LogAsync(
                new UserAuditLogRequest
                {
                    ActorUserId = actorUserId ?? "",
                    ActionType = UserAuditActionTypes.TicketResolved,
                    EntityType = nameof(TelecomTechnicalTicket),
                    EntityId = ticket.Id,
                    SummaryAr = $"إغلاق تلقائي للتذكرة {ticket.TicketNumber} بعد مزامنة HLR",
                    Payload = new { ticket.TicketNumber, msisdn, resolutionNotes },
                    IpAddress = ipAddress,
                },
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(msisdn) && !string.IsNullOrWhiteSpace(ticket.TicketNumber))
            {
                var smsBody = $"سيريتل: تم حل بلاغكم الفني رقم {ticket.TicketNumber}. شكراً لتواصلكم معنا.";
                await _sms.SendAsync(msisdn, smsBody, cancellationToken);
            }

            return (true, false);
        }

        if (ticket.Status == TechnicalTicketStatus.Open)
        {
            ticket.Status = TechnicalTicketStatus.InProgress;
            ticket.UpdatedById = actorUserId;
            _ticketRepository.Update(ticket);
            await _unitOfWork.SaveAsync(cancellationToken);
            return (false, true);
        }

        return (false, false);
    }
}
