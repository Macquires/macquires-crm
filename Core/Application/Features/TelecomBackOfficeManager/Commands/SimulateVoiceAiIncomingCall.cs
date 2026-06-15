using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public class SimulateVoiceAiIncomingCallResult
{
    public string TicketId { get; init; } = null!;
    public string TicketNumber { get; init; } = null!;
    public string Msisdn { get; init; } = null!;
    public string? CustomerId { get; init; }
    public string? SubscriberProfileId { get; init; }
    public TechnicalTicketIssueType IssueType { get; init; }
    public TechnicalTicketPriority Priority { get; init; }
    public string CreatedByChannel { get; init; } = TechnicalTicketCreatedByChannel.CustomerCareVoiceAi;
    public string SummaryAr { get; init; } = null!;
}

public class SimulateVoiceAiIncomingCallRequest : IRequest<SimulateVoiceAiIncomingCallResult>, IRequireAnyPermission
{
    public string Msisdn { get; init; } = "";
    public string RawVoiceTranscript { get; init; } = "";

    public IReadOnlyList<string> PermissionKeys => BackOfficePermissionSets.TechnicalViewAny;
}

public class SimulateVoiceAiIncomingCallValidator : AbstractValidator<SimulateVoiceAiIncomingCallRequest>
{
    public SimulateVoiceAiIncomingCallValidator()
    {
        RuleFor(x => x.Msisdn).NotEmpty();
        RuleFor(x => x.RawVoiceTranscript)
            .NotEmpty()
            .MinimumLength(5)
            .WithMessage("أدخل نص المكالمة (5 أحرف على الأقل).");
    }
}

public class SimulateVoiceAiIncomingCallHandler
    : IRequestHandler<SimulateVoiceAiIncomingCallRequest, SimulateVoiceAiIncomingCallResult>
{
    private readonly ICommandRepository<TelecomTechnicalTicket> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequence;
    private readonly IQueryContext _query;

    public SimulateVoiceAiIncomingCallHandler(
        ICommandRepository<TelecomTechnicalTicket> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequence,
        IQueryContext query)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequence = numberSequence;
        _query = query;
    }

    public async Task<SimulateVoiceAiIncomingCallResult> Handle(
        SimulateVoiceAiIncomingCallRequest request,
        CancellationToken cancellationToken)
    {
        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.Msisdn)
            ?? throw new InvalidOperationException("Invalid MSISDN.");

        var subscription = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
            .Where(s => s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == msisdn)
            .Select(s => new
            {
                s.Id,
                s.SubscriberProfileId,
                CustomerId = s.SubscriberProfile != null ? s.SubscriberProfile.CustomerId : null,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (subscription == null)
        {
            throw new InvalidOperationException("لم يُعثر على اشتراك نشط لهذا الرقم.");
        }

        var intent = VoiceAiTicketIntentParser.Parse(request.RawVoiceTranscript);

        var payload = JsonSerializer.Serialize(new
        {
            channel = TechnicalTicketCreatedByChannel.CustomerCareVoiceAi,
            rawVoiceTranscript = request.RawVoiceTranscript.Trim(),
            subscriptionId = subscription.Id,
            subscriberProfileId = subscription.SubscriberProfileId,
            customerId = subscription.CustomerId,
            inferredIssueType = intent.IssueType.ToString(),
            inferredPriority = intent.Priority.ToString(),
        });

        var entity = new TelecomTechnicalTicket
        {
            CreatedById = TechnicalTicketCreatedByChannel.VoiceAiSystemUserId,
            TicketNumber = await _numberSequence.GenerateNumberAsync(nameof(TelecomTechnicalTicket), "", "TT", cancellationToken: cancellationToken),
            Msisdn = msisdn,
            CustomerId = subscription.CustomerId,
            SubscriberProfileId = subscription.SubscriberProfileId,
            IssueType = intent.IssueType,
            TicketCategory = TechnicalTicketCategory.Complaint,
            Priority = intent.Priority,
            Status = TechnicalTicketStatus.Open,
            Notes = intent.SummaryAr,
            PayloadJson = payload,
            OpenedByUserId = TechnicalTicketCreatedByChannel.VoiceAiSystemUserId,
            CreatedByChannel = TechnicalTicketCreatedByChannel.CustomerCareVoiceAi,
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new SimulateVoiceAiIncomingCallResult
        {
            TicketId = entity.Id,
            TicketNumber = entity.TicketNumber,
            Msisdn = msisdn,
            CustomerId = subscription.CustomerId,
            SubscriberProfileId = subscription.SubscriberProfileId,
            IssueType = intent.IssueType,
            Priority = intent.Priority,
            CreatedByChannel = entity.CreatedByChannel,
            SummaryAr = intent.SummaryAr,
        };
    }
}
