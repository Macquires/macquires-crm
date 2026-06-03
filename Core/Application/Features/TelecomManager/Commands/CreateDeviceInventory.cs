using Application.Common.Exceptions;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class CreateDeviceInventoryResult
{
    public DeviceInventory? Data { get; init; }
}

public class CreateDeviceInventoryRequest : IRequest<CreateDeviceInventoryResult>
{
    public string Imei { get; init; } = null!;
    public string Model { get; init; } = null!;
    public string? Sku { get; init; }
    public decimal ListPrice { get; init; }
    public string? BranchId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateDeviceInventoryValidator : AbstractValidator<CreateDeviceInventoryRequest>
{
    public CreateDeviceInventoryValidator()
    {
        RuleFor(x => x.Imei).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ListPrice).GreaterThan(0);
    }
}

public class CreateDeviceInventoryHandler : IRequestHandler<CreateDeviceInventoryRequest, CreateDeviceInventoryResult>
{
    private readonly ICommandRepository<DeviceInventory> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDeviceInventoryHandler(ICommandRepository<DeviceInventory> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateDeviceInventoryResult> Handle(
        CreateDeviceInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var imei = request.Imei.Trim();
        var exists = _repository.GetQuery().Any(d => !d.IsDeleted && d.Imei == imei);
        if (exists)
        {
            throw new BusinessRuleViolationException("VAL-14-01: IMEI موجود مسبقاً.");
        }

        var entity = new DeviceInventory
        {
            Imei = imei,
            Model = request.Model.Trim(),
            Sku = request.Sku?.Trim(),
            ListPrice = request.ListPrice,
            BranchId = request.BranchId?.Trim(),
            Status = DeviceInventoryStatus.Available,
            CreatedById = request.CreatedById,
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        return new CreateDeviceInventoryResult { Data = entity };
    }
}
