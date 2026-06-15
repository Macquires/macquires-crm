using Application.Common.CQS.Queries;
using Application.Common.Dashboard;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.DashboardManager.Commands;

public class ReorderDashboardWidgetItem
{
    public string? Id { get; init; }
    public int SortOrder { get; init; }
}

public class ReorderDashboardWidgetsResult
{
    public int UpdatedCount { get; init; }
}

public class ReorderDashboardWidgetsRequest : IRequest<ReorderDashboardWidgetsResult>, IRequireAnyPermission
{
    public IReadOnlyList<ReorderDashboardWidgetItem> Items { get; init; } = Array.Empty<ReorderDashboardWidgetItem>();
    public IReadOnlyList<string> PermissionKeys => DashboardPermissionSets.AdminAny;
}

public class ReorderDashboardWidgetsValidator : AbstractValidator<ReorderDashboardWidgetsRequest>
{
    public ReorderDashboardWidgetsValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id).NotEmpty();
        });
    }
}

public class ReorderDashboardWidgetsHandler : IRequestHandler<ReorderDashboardWidgetsRequest, ReorderDashboardWidgetsResult>
{
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDashboardWidgetCatalogReader _catalog;
    private readonly IOperatorContext _operator;

    public ReorderDashboardWidgetsHandler(
        IQueryContext query,
        IUnitOfWork unitOfWork,
        IDashboardWidgetCatalogReader catalog,
        IOperatorContext operatorContext)
    {
        _query = query;
        _unitOfWork = unitOfWork;
        _catalog = catalog;
        _operator = operatorContext;
    }

    public async Task<ReorderDashboardWidgetsResult> Handle(
        ReorderDashboardWidgetsRequest request,
        CancellationToken cancellationToken)
    {
        var ids = request.Items.Select(i => i.Id!).Distinct().ToList();
        var entities = await _query.DashboardWidget
            .IsDeletedEqualTo()
            .Where(w => ids.Contains(w.Id))
            .ToListAsync(cancellationToken);

        if (entities.Count != ids.Count)
        {
            throw new InvalidOperationException("بعض عناصر اللوحة غير موجودة.");
        }

        var actorUserId = OperatorActor.RequireUserId(_operator);
        var orderMap = request.Items.ToDictionary(i => i.Id!, i => i.SortOrder, StringComparer.Ordinal);
        foreach (var entity in entities)
        {
            entity.SortOrder = orderMap[entity.Id];
            entity.UpdatedById = actorUserId;
        }

        await _unitOfWork.SaveAsync(cancellationToken);
        _catalog.InvalidateCache();

        return new ReorderDashboardWidgetsResult { UpdatedCount = entities.Count };
    }
}
