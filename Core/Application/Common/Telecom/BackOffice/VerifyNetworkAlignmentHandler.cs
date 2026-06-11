using Application.Common.Audit;
using Application.Common.CQS.Queries;
using Application.Common.Events;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Common.Telecom.BackOffice;

public class VerifyNetworkAlignmentHandler : INotificationHandler<SubscriberSuspendedNotification>
{
    private readonly IQueryContext _query;
    private readonly IHLRLiveStatusService _hlr;
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyNetworkAlignmentHandler> _logger;

    public VerifyNetworkAlignmentHandler(
        IQueryContext query,
        IHLRLiveStatusService hlr,
        ICommandRepository<TelecomTechnicalTicket> ticketRepo,
        IUnitOfWork unitOfWork,
        ILogger<VerifyNetworkAlignmentHandler> logger)
    {
        _query = query;
        _hlr = hlr;
        _ticketRepo = ticketRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(SubscriberSuspendedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Reactive Reconciliation: Verifying alignment for {Msisdn} due to suspension.", notification.Msisdn);

        // 1. Query HLR Live Status instantly
        var hlrStatus = await _hlr.QueryLiveStatusAsync(
            notification.Msisdn, 
            notification.Status.ToString(), 
            cancellationToken);

        // 2. CONDITION FOR ALERT: IF CRM == Suspended AND HLR == ACTIVE
        if (hlrStatus.Success && hlrStatus.IsOnline && hlrStatus.HlrSubscriberState == "ACTIVE")
        {
            _logger.LogWarning("Revenue Leakage detected reactively for MSISDN {Msisdn}. CRM: {CrmStatus}, HLR: ACTIVE.", 
                notification.Msisdn, notification.Status);

            // 3. Spawn a TelecomTechnicalTicket (RevenueAssurance)
            var openExists = await _query.TelecomTechnicalTicket.AsNoTracking()
                .AnyAsync(t => t.Msisdn == notification.Msisdn 
                               && t.TicketCategory == TechnicalTicketCategory.RevenueAssurance
                               && (t.Status == TechnicalTicketStatus.Open || t.Status == TechnicalTicketStatus.InProgress), 
                          cancellationToken);

            if (!openExists)
            {
                var ticket = new TelecomTechnicalTicket
                {
                    TicketNumber = $"RA-EVT-{DateTime.UtcNow:yyyyMMdd}-{notification.Msisdn.Substring(Math.Max(0, notification.Msisdn.Length - 4))}",
                    Msisdn = notification.Msisdn,
                    CustomerId = notification.CustomerId,
                    SubscriberProfileId = notification.SubscriberProfileId,
                    TicketCategory = TechnicalTicketCategory.RevenueAssurance,
                    IssueType = TechnicalTicketIssueType.Network,
                    Priority = TechnicalTicketPriority.High,
                    Status = TechnicalTicketStatus.Open,
                    Notes = $"[REACTIVE ALERT] Revenue Leakage: CRM status is {notification.Status} but HLR status is ACTIVE. Immediate technical sync required.",
                    CreatedById = TechnicalTicketCreatedByChannel.RevenueAssuranceSystemUserId,
                    OpenedByUserId = TechnicalTicketCreatedByChannel.RevenueAssuranceSystemUserId,
                    CreatedByChannel = TechnicalTicketCreatedByChannel.SystemJob
                };

                await _ticketRepo.CreateAsync(ticket, cancellationToken);
                await _unitOfWork.SaveAsync(cancellationToken);
                
                _logger.LogInformation("Created reactive RevenueLeakageAlert ticket {TicketNumber} for {Msisdn}.", ticket.TicketNumber, notification.Msisdn);
            }
        }
    }
}
