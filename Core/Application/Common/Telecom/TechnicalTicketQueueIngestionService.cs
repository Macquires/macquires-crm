using System.Text.Json;
using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Telecom;

public sealed class TechnicalTicketQueueIngestionService : ITechnicalTicketQueueIngestionService
{
    private readonly ICommandRepository<TelecomTechnicalTicket> _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequence;
    private readonly IQueryContext _query;

    public TechnicalTicketQueueIngestionService(
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

    public async Task<TelecomTechnicalTicket?> EnqueueFromTelecomOperationAsync(
        TelecomOperationRequest operation,
        string? actorUserId,
        CancellationToken cancellationToken = default)
    {
        var notes = (operation.Notes ?? string.Empty).Trim();
        if (!notes.StartsWith("Customer360|", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // EF Core translates string.Contains(value) to SQL; StringComparison overload is not supported.
        var operationId = operation.Id;
        var openExists = await _query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
            .AnyAsync(
                t => (t.Status == TechnicalTicketStatus.Open || t.Status == TechnicalTicketStatus.InProgress)
                     && t.PayloadJson != null
                     && t.PayloadJson.Contains(operationId),
                cancellationToken);
        if (openExists)
        {
            return null;
        }

        var msisdn = await ResolveMsisdnAsync(operation, cancellationToken);
        if (string.IsNullOrEmpty(msisdn))
        {
            return null;
        }

        var category = TechnicalTicketCategoryMapper.FromTelecomOperationKind(operation.Kind);
        var customerId = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.Id == operation.SubscriberProfileId)
            .Select(p => p.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            telecomOperationRequestId = operation.Id,
            operationNumber = operation.Number,
            operationKind = operation.Kind.ToString(),
            ticketCategory = category.ToString(),
            targetOfferName = operation.TargetOfferName,
            productOfferingId = operation.ProductOfferingId,
        });

        var actor = string.IsNullOrWhiteSpace(actorUserId) ? operation.CreatedById : actorUserId.Trim();
        if (string.IsNullOrEmpty(actor))
        {
            actor = "system-showroom";
        }

        var entity = new TelecomTechnicalTicket
        {
            CreatedById = actor,
            TicketNumber = _numberSequence.GenerateNumber(nameof(TelecomTechnicalTicket), "", "TT"),
            Msisdn = msisdn,
            CustomerId = customerId,
            SubscriberProfileId = operation.SubscriberProfileId,
            TicketCategory = category,
            IssueType = TechnicalTicketCategoryMapper.DefaultIssueType(category),
            Priority = TechnicalTicketCategoryMapper.DefaultPriority(category),
            Status = TechnicalTicketStatus.Open,
            Notes = BuildQueueNotes(operation, category),
            PayloadJson = payload,
            OpenedByUserId = actor,
            CreatedByChannel = TechnicalTicketCreatedByChannel.ShowroomAgent,
        };

        await _ticketRepository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        return entity;
    }

    private static string BuildQueueNotes(TelecomOperationRequest operation, TechnicalTicketCategory category)
    {
        var label = TechnicalTicketCategoryMapper.CategoryLabelAr(category);
        var opNo = operation.Number ?? "—";
        return $"{label} — طلب {opNo} من Customer 360 | {operation.Notes}".Trim();
    }

    private async Task<string?> ResolveMsisdnAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(operation.MsisdnAssetId))
        {
            var fromAsset = await _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo()
                .Where(m => m.Id == operation.MsisdnAssetId)
                .Select(m => m.Msisdn)
                .FirstOrDefaultAsync(cancellationToken);
            var canonical = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(fromAsset ?? "");
            if (!string.IsNullOrEmpty(canonical))
            {
                return canonical;
            }
        }

        var fromSub = await (
            from s in _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            join m in _query.MsisdnAsset.AsNoTracking().IsDeletedEqualTo() on s.MsisdnAssetId equals m.Id
            where s.SubscriberProfileId == operation.SubscriberProfileId
            orderby s.IsPrimaryLine descending
            select m.Msisdn)
            .FirstOrDefaultAsync(cancellationToken);

        return TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(fromSub ?? "");
    }
}
