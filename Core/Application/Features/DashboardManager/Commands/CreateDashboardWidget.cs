using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Commands;

public class CreateDashboardWidgetResult
{
    public DashboardWidget? Data { get; set; }
}

public class CreateDashboardWidgetRequest : IRequest<CreateDashboardWidgetResult>, IRequireAnyPermission
{
    public string? WidgetKey { get; init; }
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
    public IReadOnlyList<string> PermissionKeys => DashboardPermissionSets.AdminAny;
}

public class CreateDashboardWidgetValidator : AbstractValidator<CreateDashboardWidgetRequest>
{
    public CreateDashboardWidgetValidator()
    {
        RuleFor(x => x.WidgetKey).NotEmpty().MaximumLength(128);
        RuleFor(x => x.TitleAr).NotEmpty();
        RuleFor(x => x.PersonasAllowed).NotEmpty();
    }
}

public class CreateDashboardWidgetHandler : IRequestHandler<CreateDashboardWidgetRequest, CreateDashboardWidgetResult>
{
    private readonly ICommandRepository<DashboardWidget> _repository;
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDashboardWidgetRegistry _registry;
    private readonly IDashboardWidgetCatalogReader _catalog;
    private readonly IOperatorContext _operator;

    public CreateDashboardWidgetHandler(
        ICommandRepository<DashboardWidget> repository,
        IQueryContext query,
        IUnitOfWork unitOfWork,
        IDashboardWidgetRegistry registry,
        IDashboardWidgetCatalogReader catalog,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _query = query;
        _unitOfWork = unitOfWork;
        _registry = registry;
        _catalog = catalog;
        _operator = operatorContext;
    }

    public async Task<CreateDashboardWidgetResult> Handle(
        CreateDashboardWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var widgetKey = DashboardWidgetAdminRules.NormalizeWidgetKey(request.WidgetKey);
        if (!System.Text.RegularExpressions.Regex.IsMatch(widgetKey, @"^[a-z][a-z0-9_]*$"))
        {
            throw new InvalidOperationException("مفتاح الكرت يجب أن يبدأ بحرف ويحتوي a-z و 0-9 و _ فقط.");
        }

        var personas = (request.PersonasAllowed ?? string.Empty).Trim();
        DashboardWidgetAdminRules.ValidatePersonasCsv(personas);
        DashboardWidgetAdminRules.ValidateProviderKey(
            _registry,
            request.ProviderKey,
            request.WidgetKind);

        var exists = await _query.DashboardWidget
            .AsNoTracking()
            .IsDeletedEqualTo()
            .AnyAsync(w => w.WidgetKey == widgetKey, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("مفتاح الكرت مستخدم مسبقاً.");
        }

        var entity = new DashboardWidget
        {
            CreatedById = OperatorActor.RequireUserId(_operator),
            WidgetKey = widgetKey,
            TitleAr = (request.TitleAr ?? string.Empty).Trim(),
            TitleEn = string.IsNullOrWhiteSpace(request.TitleEn) ? null : request.TitleEn.Trim(),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            ProviderKey = string.IsNullOrWhiteSpace(request.ProviderKey) ? null : request.ProviderKey.Trim(),
            PersonasAllowed = personas,
            GridSize = request.GridSize,
            SortOrder = request.SortOrder,
            WidgetKind = request.WidgetKind,
            RefreshIntervalSeconds = request.RefreshIntervalSeconds,
            CtaUrl = string.IsNullOrWhiteSpace(request.CtaUrl) ? null : request.CtaUrl.Trim(),
            CtaLabelAr = string.IsNullOrWhiteSpace(request.CtaLabelAr) ? null : request.CtaLabelAr.Trim(),
            CtaLabelEn = string.IsNullOrWhiteSpace(request.CtaLabelEn) ? null : request.CtaLabelEn.Trim(),
            IsActive = request.IsActive,
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        _catalog.InvalidateCache();

        return new CreateDashboardWidgetResult { Data = entity };
    }
}
