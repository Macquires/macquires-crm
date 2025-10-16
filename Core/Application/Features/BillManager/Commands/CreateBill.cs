using Application.Common.Repositories;
using Application.Features.NumberSequenceManager;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.BillManager.Commands;

public class CreateBillResult
{
    public Bill? Data { get; set; }
}

public class CreateBillRequest : IRequest<CreateBillResult>
{
    public DateTime? BillDate { get; init; }
    public string? BillStatus { get; init; }
    public string? Description { get; init; }
    public string? PurchaseOrderId { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateBillValidator : AbstractValidator<CreateBillRequest>
{
    public CreateBillValidator()
    {
        RuleFor(x => x.BillDate).NotNull();
        RuleFor(x => x.BillStatus).NotEmpty();
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
    }
}

public class CreateBillHandler : IRequestHandler<CreateBillRequest, CreateBillResult>
{
    private readonly ICommandRepository<Bill> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NumberSequenceService _numberSequenceService;

    public CreateBillHandler(
        ICommandRepository<Bill> repository,
        IUnitOfWork unitOfWork,
        NumberSequenceService numberSequenceService
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _numberSequenceService = numberSequenceService;
    }

    public async Task<CreateBillResult> Handle(CreateBillRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Bill
        {
            CreatedById = request.CreatedById,
            Number = _numberSequenceService.GenerateNumber(nameof(Bill), "", "BLL"),
            BillDate = request.BillDate,
            BillStatus = (BillStatus)int.Parse(request.BillStatus!),
            Description = request.Description,
            PurchaseOrderId = request.PurchaseOrderId
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateBillResult
        {
            Data = entity
        };
    }
}