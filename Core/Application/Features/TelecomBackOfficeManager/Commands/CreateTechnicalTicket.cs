using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomBackOfficeManager.Commands;

public class CreateTechnicalTicketResult
{
    public TelecomTechnicalTicket? Data { get; init; }
}

public class CreateTechnicalTicketRequest : IRequest<CreateTechnicalTicketResult>
{
    public string Msisdn { get; init; } = "";
    public TechnicalTicketIssueType IssueType { get; init; }
    public TechnicalTicketCategory? TicketCategory { get; init; }
    public TechnicalTicketPriority Priority { get; init; } = TechnicalTicketPriority.Medium;
    public string? Notes { get; init; }
    public string? PayloadJson { get; init; }
    public string? CustomerId { get; init; }
    public string? SubscriberProfileId { get; init; }
    public string? OpenedByUserId { get; init; }
    public string? CreatedByChannel { get; init; }
}

public class CreateTechnicalTicketValidator : AbstractValidator<CreateTechnicalTicketRequest>
{
    public CreateTechnicalTicketValidator()
    {
        RuleFor(x => x.Msisdn).NotEmpty();
        RuleFor(x => x.OpenedByUserId).NotEmpty();
    }
}

public class CreateTechnicalTicketHandler : IRequestHandler<CreateTechnicalTicketRequest, CreateTechnicalTicketResult>
{
    private readonly ICommandRepository<TelecomTechnicalTicket> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequence;
    private readonly IQueryContext _query;

    public CreateTechnicalTicketHandler(
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

    public async Task<CreateTechnicalTicketResult> Handle(CreateTechnicalTicketRequest request, CancellationToken cancellationToken)
    {
        var msisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(request.Msisdn)
            ?? throw new InvalidOperationException("Invalid MSISDN.");

        string? customerId = request.CustomerId;
        string? profileId = request.SubscriberProfileId;

        if (string.IsNullOrEmpty(profileId))
        {
            profileId = await _query.TelecomSubscription.AsNoTracking().IsDeletedEqualTo()
                .Where(s => s.MsisdnAsset != null && s.MsisdnAsset.Msisdn == msisdn)
                .Select(s => s.SubscriberProfileId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (string.IsNullOrEmpty(customerId) && !string.IsNullOrEmpty(profileId))
        {
            customerId = await _query.SubscriberProfile.AsNoTracking().IsDeletedEqualTo()
                .Where(p => p.Id == profileId)
                .Select(p => p.CustomerId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var entity = new TelecomTechnicalTicket
        {
            CreatedById = request.OpenedByUserId,
            TicketNumber = _numberSequence.GenerateNumber(nameof(TelecomTechnicalTicket), "", "TT"),
            Msisdn = msisdn,
            CustomerId = customerId,
            SubscriberProfileId = profileId,
            IssueType = request.IssueType,
            TicketCategory = request.TicketCategory ?? TechnicalTicketCategory.Complaint,
            Priority = request.Priority,
            Status = TechnicalTicketStatus.Open,
            Notes = request.Notes,
            PayloadJson = request.PayloadJson,
            OpenedByUserId = request.OpenedByUserId!,
            CreatedByChannel = string.IsNullOrWhiteSpace(request.CreatedByChannel)
                ? TechnicalTicketCreatedByChannel.CallCenterAgent
                : request.CreatedByChannel.Trim(),
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateTechnicalTicketResult { Data = entity };
    }
}
