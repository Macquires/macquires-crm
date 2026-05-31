using Application.Common.Dashboard;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.DashboardManager.Commands;

public class UpdateDashboardWidgetResult
{
    public DashboardWidget? Data { get; set; }
}

public class UpdateDashboardWidgetRequest : IRequest<UpdateDashboardWidgetResult>
{
    public string? Id { get; init; }
    public string? TitleAr { get; init; }
    public string? TitleEn { get; init; }
    public string? Icon { get; init; }
    public string? ProviderKey { get; init; }
    public string? PersonasAllowed { get; init; }
    public DashboardWidgetGridSize GridSize { get; init; } = DashboardWidgetGridSize.Medium;
    public int SortOrder { get; init; }
    public DashboardWidgetKind WidgetKind { get; init; } = DashboardWidgetKind.Stat;
    public int? RefreshIntervalSeconds { get; init; }
    public string? CtaUrl { get; init; }
    public string? CtaLabelAr { get; init; }
    public string? CtaLabelEn { get; init; }
    public bool IsActive { get; init; } = true;
    public string? UpdatedById { get; init; }
}

public class UpdateDashboardWidgetValidator : AbstractValidator<UpdateDashboardWidgetRequest>
{
    public UpdateDashboardWidgetValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.TitleAr).NotEmpty();
        RuleFor(x => x.PersonasAllowed).NotEmpty();
    }
}

public class UpdateDashboardWidgetHandler : IRequestHandler<UpdateDashboardWidgetRequest, UpdateDashboardWidgetResult>
{
    private readonly ICommandRepository<DashboardWidget> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDashboardWidgetRegistry _registry;
    private readonly IDashboardWidgetCatalogReader _catalog;

    public UpdateDashboardWidgetHandler(
        ICommandRepository<DashboardWidget> repository,
        IUnitOfWork unitOfWork,
        IDashboardWidgetRegistry registry,
        IDashboardWidgetCatalogReader catalog)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _registry = registry;
        _catalog = catalog;
    }

    public async Task<UpdateDashboardWidgetResult> Handle(
        UpdateDashboardWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id!, cancellationToken)
            ?? throw new InvalidOperationException("عنصر اللوحة غير موجود.");

        var personas = (request.PersonasAllowed ?? string.Empty).Trim();
        DashboardWidgetAdminRules.ValidatePersonasCsv(personas);
        DashboardWidgetAdminRules.ValidateProviderKey(
            _registry,
            request.ProviderKey,
            request.WidgetKind);

        entity.UpdatedById = request.UpdatedById;
        entity.TitleAr = (request.TitleAr ?? string.Empty).Trim();
        entity.TitleEn = string.IsNullOrWhiteSpace(request.TitleEn) ? null : request.TitleEn.Trim();
        entity.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        entity.ProviderKey = string.IsNullOrWhiteSpace(request.ProviderKey) ? null : request.ProviderKey.Trim();
        entity.PersonasAllowed = personas;
        entity.GridSize = request.GridSize;
        entity.SortOrder = request.SortOrder;
        entity.WidgetKind = request.WidgetKind;
        entity.RefreshIntervalSeconds = request.RefreshIntervalSeconds;
        entity.CtaUrl = string.IsNullOrWhiteSpace(request.CtaUrl) ? null : request.CtaUrl.Trim();
        entity.CtaLabelAr = string.IsNullOrWhiteSpace(request.CtaLabelAr) ? null : request.CtaLabelAr.Trim();
        entity.CtaLabelEn = string.IsNullOrWhiteSpace(request.CtaLabelEn) ? null : request.CtaLabelEn.Trim();
        entity.IsActive = request.IsActive;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);
        _catalog.InvalidateCache();

        return new UpdateDashboardWidgetResult { Data = entity };
    }
}
