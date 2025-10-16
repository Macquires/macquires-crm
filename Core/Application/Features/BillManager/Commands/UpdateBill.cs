using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.BillManager.Commands;

public class UpdateBillResult
{
    public Bill? Data { get; set; }
}

public class UpdateBillRequest : IRequest<UpdateBillResult>
{
    public string? Id { get; init; }
    public DateTime? BillDate { get; init; }
    public string? BillStatus { get; init; }
    public string? Description { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateBillValidator : AbstractValidator<UpdateBillRequest>
{
    public UpdateBillValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BillDate).NotNull();
        RuleFor(x => x.BillStatus).NotEmpty();
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
    }
}

public class UpdateBillHandler : IRequestHandler<UpdateBillRequest, UpdateBillResult>
{
    private readonly ICommandRepository<Bill> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBillHandler(
        ICommandRepository<Bill> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateBillResult> Handle(UpdateBillRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null)
        {
            throw new Exception($"Entity not found: {request.Id}");
        }

        entity.UpdatedById = request.UpdatedById;
        entity.BillDate = request.BillDate;
        entity.BillStatus = (BillStatus)int.Parse(request.BillStatus!);
        entity.Description = request.Description;
        entity.PurchaseOrderId = request.PurchaseOrderId;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateBillResult
        {
            Data = entity
        };
    }
}