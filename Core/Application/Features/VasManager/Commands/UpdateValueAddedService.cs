using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.VasManager.Commands;

public class UpdateValueAddedServiceResult
{
    public TelecomValueAddedService? Data { get; set; }
}

public class UpdateValueAddedServiceRequest : IRequest<UpdateValueAddedServiceResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.TelecomVasManage;
    public string Id { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? NameEn { get; init; }
    public string? Description { get; init; }
    public decimal MonthlyFee { get; init; }
    public bool IsActive { get; init; } = true;
    public string HlrCommandTemplate { get; init; } = "";
    public int SortOrder { get; init; }
}

public class UpdateValueAddedServiceValidator : AbstractValidator<UpdateValueAddedServiceRequest>
{
    public UpdateValueAddedServiceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(255);
        RuleFor(x => x.HlrCommandTemplate).NotEmpty().MaximumLength(512);
        RuleFor(x => x.MonthlyFee).GreaterThanOrEqualTo(0);
    }
}

public class UpdateValueAddedServiceHandler : IRequestHandler<UpdateValueAddedServiceRequest, UpdateValueAddedServiceResult>
{
    private readonly ICommandRepository<TelecomValueAddedService> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public UpdateValueAddedServiceHandler(
        ICommandRepository<TelecomValueAddedService> repository,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<UpdateValueAddedServiceResult> Handle(
        UpdateValueAddedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("VAS service not found.");

        entity.NameAr = request.NameAr.Trim();
        entity.NameEn = request.NameEn?.Trim();
        entity.Description = request.Description?.Trim();
        entity.MonthlyFee = request.MonthlyFee;
        entity.IsActive = request.IsActive;
        entity.HlrCommandTemplate = request.HlrCommandTemplate.Trim();
        entity.SortOrder = request.SortOrder;
        entity.UpdatedById = OperatorActor.RequireUserId(_operator);
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);
        return new UpdateValueAddedServiceResult { Data = entity };
    }
}
