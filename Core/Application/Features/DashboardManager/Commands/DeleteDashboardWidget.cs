using Application.Common.Dashboard;
using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.DashboardManager.Commands;

public class DeleteDashboardWidgetResult
{
    public DashboardWidget? Data { get; set; }
}

public class DeleteDashboardWidgetRequest : IRequest<DeleteDashboardWidgetResult>, IRequireAnyPermission
{
    public string? Id { get; init; }
    public IReadOnlyList<string> PermissionKeys => DashboardPermissionSets.AdminAny;
}

public class DeleteDashboardWidgetValidator : AbstractValidator<DeleteDashboardWidgetRequest>
{
    public DeleteDashboardWidgetValidator() => RuleFor(x => x.Id).NotEmpty();
}

public class DeleteDashboardWidgetHandler : IRequestHandler<DeleteDashboardWidgetRequest, DeleteDashboardWidgetResult>
{
    private readonly ICommandRepository<DashboardWidget> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDashboardWidgetCatalogReader _catalog;
    private readonly IOperatorContext _operator;

    public DeleteDashboardWidgetHandler(
        ICommandRepository<DashboardWidget> repository,
        IUnitOfWork unitOfWork,
        IDashboardWidgetCatalogReader catalog,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _catalog = catalog;
        _operator = operatorContext;
    }

    public async Task<DeleteDashboardWidgetResult> Handle(
        DeleteDashboardWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id!, cancellationToken)
            ?? throw new InvalidOperationException("عنصر اللوحة غير موجود.");

        entity.UpdatedById = OperatorActor.RequireUserId(_operator);
        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);
        _catalog.InvalidateCache();

        return new DeleteDashboardWidgetResult { Data = entity };
    }
}
