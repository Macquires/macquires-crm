using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Features.NumberSequenceManager;
using Application.Features.TelecomManager.Commands;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public sealed class ForceCbsSyncResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public string? TicketNumber { get; init; }
    public string? Msisdn { get; init; }
    public string? OperationNumber { get; init; }
    public decimal? BalanceAfterSync { get; init; }
    public bool TicketAutoResolved { get; init; }
}

public class ForceCbsSyncByTicketRequest : IRequest<ForceCbsSyncResult>, IRequireAnyPermission
{
    public string TicketId { get; init; } = "";
    public string? IpAddress { get; init; }
    public IReadOnlyList<string> PermissionKeys => BackOfficePermissionSets.TechnicalSyncAny;
}

public class ForceCbsSyncByTicketValidator : AbstractValidator<ForceCbsSyncByTicketRequest>
{
    public ForceCbsSyncByTicketValidator() => RuleFor(x => x.TicketId).NotEmpty();
}

public class ForceCbsSyncByTicketHandler : IRequestHandler<ForceCbsSyncByTicketRequest, ForceCbsSyncResult>
{
    private readonly IQueryContext _query;
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBillingSystemIntegration _billing;
    private readonly IUserAuditService _audit;
    private readonly ISmsGatewayIntegration _sms;
    private readonly NumberSequenceService _numberSequenceService;
    private readonly IOperatorContext _operator;

    public ForceCbsSyncByTicketHandler(
        IQueryContext query,
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<TelecomTechnicalTicket> ticketRepository,
        IUnitOfWork unitOfWork,
        IBillingSystemIntegration billing,
        IUserAuditService audit,
        ISmsGatewayIntegration sms,
        NumberSequenceService numberSequenceService,
        IOperatorContext operatorContext)
    {
        _query = query;
        _operationRepository = operationRepository;
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _billing = billing;
        _audit = audit;
        _sms = sms;
        _numberSequenceService = numberSequenceService;
        _operator = operatorContext;
    }

    public async Task<ForceCbsSyncResult> Handle(ForceCbsSyncByTicketRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);

        var ticket = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .Where(t => t.Id == request.TicketId)
            .Select(t => new { t.Id, t.TicketNumber, t.Msisdn, t.IssueType, t.SubscriberProfileId, t.CustomerId, t.BranchId })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("التذكرة غير موجودة.");

        var line = await TechnicalTicketLineResolver.ResolveForBackOfficeTicketAsync(
            _query,
            ticket.SubscriberProfileId,
            ticket.Msisdn,
            cancellationToken);

        await EnsureTicketLinkedAsync(ticket.Id, line, actorUserId, cancellationToken);

        var priorSuccessLog = await _query.BillingIntegrationLog.AsNoTracking().IsDeletedEqualTo()
            .Where(l => l.Success && l.CorrelationId == ticket.Id)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Select(l => new { l.Message, l.TelecomOperationRequestId })
            .FirstOrDefaultAsync(cancellationToken);

        if (priorSuccessLog != null)
        {
            var replayBalance = await _billing.GetOutstandingBalanceAsync(line.Msisdn, cancellationToken);
            var existingOpNumber = priorSuccessLog.TelecomOperationRequestId == null
                ? null
                : await _query.TelecomOperationRequest.AsNoTracking().IsDeletedEqualTo()
                    .Where(o => o.Id == priorSuccessLog.TelecomOperationRequestId)
                    .Select(o => o.Number)
                    .FirstOrDefaultAsync(cancellationToken);

            var replayResolved = await AutoResolveTicketAfterCbsAsync(
                ticket.Id,
                ticket.TicketNumber,
                line.Msisdn,
                priorSuccessLog.Message ?? "CBS already synced for this ticket.",
                actorUserId,
                request.IpAddress,
                cancellationToken);

            return new ForceCbsSyncResult
            {
                Success = true,
                Message = priorSuccessLog.Message ?? "تمت تسوية CBS مسبقاً لهذه التذكرة.",
                TicketNumber = ticket.TicketNumber,
                Msisdn = line.Msisdn,
                OperationNumber = existingOpNumber,
                BalanceAfterSync = replayBalance,
                TicketAutoResolved = replayResolved,
            };
        }

        var (entityName, prefix) = TelecomNumberSequence.ForKind(TelecomOperationKind.ServiceModification);
        var operationNumber = await _numberSequenceService.GenerateNumberAsync(entityName, prefix, "", useDate: false, cancellationToken: cancellationToken);

        var operation = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.ServiceModification,
            Number = operationNumber,
            CorrelationId = ticket.Id,
            Status = TelecomOperationStatus.Confirmed,
            DocumentStatus = TelecomDocumentStatus.Verified,
            SubscriberProfileId = line.SubscriberProfileId,
            MsisdnAssetId = line.MsisdnAssetId,
            ProductId = line.ProductId,
            Notes = $"Force CBS sync from ticket {ticket.TicketNumber}",
            ConfirmedAtUtc = DateTime.UtcNow,
            CreatedById = actorUserId,
            BranchId = ticket.BranchId,
        };

        await _operationRepository.CreateAsync(operation, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var provision = await _billing.ProvisionAsync(
            new BillingProvisionRequest(
                operation.Id,
                operation.Number,
                line.Msisdn,
                TelecomOperationKind.ServiceModification,
                CorrelationId: ticket.Id,
                BranchId: operation.BranchId),
            cancellationToken);

        if (!provision.Success)
        {
            operation.Status = TelecomOperationStatus.Failed;
            operation.Notes = $"{operation.Notes} | CBS failed: {provision.Message}";
            _operationRepository.Update(operation);
            await _unitOfWork.SaveAsync(cancellationToken);
            throw new InvalidOperationException(provision.Message);
        }

        operation.Status = TelecomOperationStatus.Completed;
        operation.UpdatedById = actorUserId;
        _operationRepository.Update(operation);
        await _unitOfWork.SaveAsync(cancellationToken);

        var balance = await _billing.GetOutstandingBalanceAsync(line.Msisdn, cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId,
                ActionType = UserAuditActionTypes.NetworkCommandExecuted,
                EntityType = "Huawei_CBS",
                EntityId = ticket.Id,
                SummaryAr = $"إعادة ضغط الشحنة وتسوية CBS للخط {line.Msisdn} (تذكرة {ticket.TicketNumber})",
                Payload = new
                {
                    ticket.TicketNumber,
                    msisdn = line.Msisdn,
                    ticket.IssueType,
                    operationNumber,
                    operationId = operation.Id,
                    provision.Message,
                    balanceAfterSync = balance,
                },
                IpAddress = request.IpAddress,
            },
            cancellationToken);

        var ticketResolved = await AutoResolveTicketAfterCbsAsync(
            ticket.Id,
            ticket.TicketNumber,
            line.Msisdn,
            provision.Message,
            actorUserId,
            request.IpAddress,
            cancellationToken);

        return new ForceCbsSyncResult
        {
            Success = true,
            Message = provision.Message,
            TicketNumber = ticket.TicketNumber,
            Msisdn = line.Msisdn,
            OperationNumber = operationNumber,
            BalanceAfterSync = balance,
            TicketAutoResolved = ticketResolved,
        };
    }

    private async Task<bool> AutoResolveTicketAfterCbsAsync(
        string ticketId,
        string ticketNumber,
        string msisdn,
        string provisionMessage,
        string actorUserId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var ticketEntity = await _ticketRepository.GetAsync(ticketId, cancellationToken);
        if (ticketEntity == null || ticketEntity.Status == TechnicalTicketStatus.Resolved)
        {
            return false;
        }

        var resolutionNotes = $"تم دفع الشحنة وتسوية الفوترة على CBS — {provisionMessage}";
        ticketEntity.Status = TechnicalTicketStatus.Resolved;
        ticketEntity.ResolutionNotes = resolutionNotes;
        ticketEntity.ResolvedByUserId = actorUserId;
        ticketEntity.ResolvedAtUtc = DateTime.UtcNow;
        ticketEntity.UpdatedById = actorUserId;
        _ticketRepository.Update(ticketEntity);
        await _unitOfWork.SaveAsync(cancellationToken);

        await _audit.LogAsync(
            new UserAuditLogRequest
            {
                ActorUserId = actorUserId,
                ActionType = UserAuditActionTypes.TicketResolved,
                EntityType = nameof(TelecomTechnicalTicket),
                EntityId = ticketEntity.Id,
                SummaryAr = $"إغلاق تلقائي للتذكرة {ticketNumber} بعد تسوية CBS",
                Payload = new { ticketNumber, msisdn, resolutionNotes },
                IpAddress = ipAddress,
            },
            cancellationToken);

        if (!string.IsNullOrWhiteSpace(msisdn))
        {
            var smsBody = $"سيريتل: تم حل بلاغكم الفني رقم {ticketNumber}. شكراً لتواصلكم معنا.";
            await _sms.SendAsync(msisdn, smsBody, cancellationToken);
        }

        return true;
    }

    private async Task EnsureTicketLinkedAsync(
        string ticketId,
        TechnicalTicketLineContext line,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        var entity = await _ticketRepository.GetAsync(ticketId, cancellationToken);
        if (entity == null)
        {
            return;
        }

        if (entity.SubscriberProfileId == line.SubscriberProfileId
            && entity.Msisdn == line.Msisdn
            && entity.CustomerId == line.CustomerId)
        {
            return;
        }

        entity.SubscriberProfileId = line.SubscriberProfileId;
        entity.CustomerId = line.CustomerId;
        entity.Msisdn = line.Msisdn;
        entity.UpdatedById = actorUserId;
        _ticketRepository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
