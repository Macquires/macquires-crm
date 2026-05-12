using Application.Common.Integrations;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Features.TelecomManager.Commands;

public class ConfirmTelecomOperationRequestResult
{
    public TelecomOperationRequest? Data { get; set; }
    public BillingProvisionResult? BillingResult { get; set; }
}

public class ConfirmTelecomOperationRequest : IRequest<ConfirmTelecomOperationRequestResult>
{
    public string Id { get; init; } = null!;
    public string? UpdatedById { get; init; }
}

public class ConfirmTelecomOperationRequestValidator : AbstractValidator<ConfirmTelecomOperationRequest>
{
    public ConfirmTelecomOperationRequestValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class ConfirmTelecomOperationRequestHandler : IRequestHandler<ConfirmTelecomOperationRequest, ConfirmTelecomOperationRequestResult>
{
    private readonly ICommandRepository<TelecomOperationRequest> _operationRepository;
    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBillingSystemIntegration _billing;
    private readonly ILogger<ConfirmTelecomOperationRequestHandler> _logger;

    public ConfirmTelecomOperationRequestHandler(
        ICommandRepository<TelecomOperationRequest> operationRepository,
        ICommandRepository<MsisdnAsset> msisdnRepository,
        IUnitOfWork unitOfWork,
        IBillingSystemIntegration billing,
        ILogger<ConfirmTelecomOperationRequestHandler> logger)
    {
        _operationRepository = operationRepository;
        _msisdnRepository = msisdnRepository;
        _unitOfWork = unitOfWork;
        _billing = billing;
        _logger = logger;
    }

    public async Task<ConfirmTelecomOperationRequestResult> Handle(ConfirmTelecomOperationRequest request, CancellationToken cancellationToken)
    {
        var entity = await _operationRepository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Telecom operation not found.");

        if (entity.Status != TelecomOperationStatus.Draft)
            throw new InvalidOperationException("Only draft operations can be confirmed.");

        if (entity.DocumentStatus < TelecomDocumentStatus.Uploaded)
            throw new InvalidOperationException("Identity document must be uploaded before confirmation.");

        entity.Status = TelecomOperationStatus.Confirmed;
        entity.ConfirmedAtUtc = DateTime.UtcNow;
        entity.UpdatedById = request.UpdatedById;
        _operationRepository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        entity.Status = TelecomOperationStatus.PendingExternal;
        _operationRepository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        string? msisdn = null;
        if (!string.IsNullOrEmpty(entity.MsisdnAssetId))
        {
            var asset = await _msisdnRepository.GetAsync(entity.MsisdnAssetId, cancellationToken);
            msisdn = asset?.Msisdn;
        }

        var provisionRequest = new BillingProvisionRequest(entity.Id, entity.Number, msisdn, entity.Kind);
        var billingResult = await _billing.ProvisionAsync(provisionRequest, cancellationToken);

        if (billingResult.Success)
        {
            entity.Status = TelecomOperationStatus.Completed;
        }
        else
        {
            entity.Status = TelecomOperationStatus.PendingExternal;
        }

        _operationRepository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        _logger.LogInformation("Telecom operation {Number} billing outcome: {Message}", entity.Number, billingResult.Message);

        return new ConfirmTelecomOperationRequestResult
        {
            Data = entity,
            BillingResult = billingResult
        };
    }
}
