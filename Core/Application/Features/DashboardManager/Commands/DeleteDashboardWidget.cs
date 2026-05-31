using Application.Common.Dashboard;
using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;

namespace Application.Features.DashboardManager.Commands;

public class DeleteDashboardWidgetResult
{
    public DashboardWidget? Data { get; set; }
}

public class DeleteDashboardWidgetRequest : IRequest<DeleteDashboardWidgetResult>
{
    public string? Id { get; init; }
    public string? DeletedById { get; init; }
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

    public DeleteDashboardWidgetHandler(
        ICommandRepository<DashboardWidget> repository,
        IUnitOfWork unitOfWork,
        IDashboardWidgetCatalogReader catalog)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _catalog = catalog;
    }

    public async Task<DeleteDashboardWidgetResult> Handle(
        DeleteDashboardWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id!, cancellationToken)
            ?? throw new InvalidOperationException("عنصر اللوحة غير موجود.");

        entity.UpdatedById = request.DeletedById;
        _repository.Delete(entity);
        await _unitOfWork.SaveAsync(cancellationToken);
        _catalog.InvalidateCache();

        return new DeleteDashboardWidgetResult { Data = entity };
    }
}
