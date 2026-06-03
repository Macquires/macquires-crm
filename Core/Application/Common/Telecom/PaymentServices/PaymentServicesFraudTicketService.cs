using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom.PaymentServices;

public interface IPaymentServicesFraudTicketService
{
    Task EnqueueRechargeVelocityFraudTicketAsync(
        string msisdn,
        string? customerId,
        string? subscriberProfileId,
        int rechargeCount,
        string? actorUserId,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentServicesFraudTicketService : IPaymentServicesFraudTicketService
{
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequence;
    private readonly IQueryContext _query;

    public PaymentServicesFraudTicketService(
        ICommandRepository<TelecomTechnicalTicket> ticketRepository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequence,
        IQueryContext query)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _numberSequence = numberSequence;
        _query = query;
    }

    public async Task EnqueueRechargeVelocityFraudTicketAsync(
        string msisdn,
        string? customerId,
        string? subscriberProfileId,
        int rechargeCount,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var payloadMarker = $"FRAUD-PAYMENT|{msisdn}";
        var openExists = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(
                t => (t.Status == TechnicalTicketStatus.Open || t.Status == TechnicalTicketStatus.InProgress)
                     && t.PayloadJson != null
                     && t.PayloadJson.Contains(payloadMarker),
                cancellationToken);
        if (openExists)
        {
            return;
        }

        var actor = string.IsNullOrWhiteSpace(actorUserId) ? "system-payment-fraud" : actorUserId.Trim();
        var entity = new TelecomTechnicalTicket
        {
            CreatedById = actor,
            TicketNumber = _numberSequence.GenerateNumber(nameof(TelecomTechnicalTicket), "", "TT"),
            Msisdn = msisdn,
            CustomerId = customerId,
            SubscriberProfileId = subscriberProfileId,
            TicketCategory = TechnicalTicketCategory.FraudPayment,
            IssueType = TechnicalTicketIssueType.Billing,
            Priority = TechnicalTicketPriority.Critical,
            Status = TechnicalTicketStatus.Open,
            Notes = $"VAL-12-04: {rechargeCount} عمليات شحن خلال {PaymentServicesConstants.FraudWindowMinutes} دقيقة — خط محجوب.",
            PayloadJson = JsonSerializer.Serialize(new
            {
                marker = payloadMarker,
                rechargeCount,
                windowMinutes = PaymentServicesConstants.FraudWindowMinutes,
            }),
            OpenedByUserId = actor,
            CreatedByChannel = TechnicalTicketCreatedByChannel.ShowroomAgent,
        };

        await _ticketRepository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
    }
}
