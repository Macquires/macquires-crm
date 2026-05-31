using Application.Common.Dashboard;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Application.Features.DashboardManager.Queries;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Dashboard;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.SecurityManager.Roles;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Dashboard;

public class GetMyDashboardWidgetsHandlerTests
{
    public GetMyDashboardWidgetsHandlerTests() => DashboardTestEncryption.EnsureInitialized();

    [Fact]
    public async Task Retail_role_does_not_receive_executive_only_widgets()
    {
        await using var ctx = CreateContext();
        ctx.DashboardWidget.AddRange(
            Widget("exec_kpi", "Executive", "subscriber_count"),
            Widget("retail_kpi", "Retail", "operations_today"),
            Widget("shared_cta", "Executive,Retail", null, DashboardWidgetKind.Cta));
        await ctx.SaveChangesAsync();

        var registry = new DashboardWidgetRegistry(
        [
            new StubProvider("subscriber_count"),
            new StubProvider("operations_today"),
        ]);
        var handler2 = new GetMyDashboardWidgetsHandler(
            new TestCatalogReader(ctx),
            registry,
            new NullOperatorContext());

        var result = await handler2.Handle(
            new GetMyDashboardWidgetsRequest { Roles = [TelecomRoles.Showroom] },
            CancellationToken.None);

        var keys = result.Data!.Select(w => w.WidgetKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("retail_kpi", keys);
        Assert.Contains("shared_cta", keys);
        Assert.DoesNotContain("exec_kpi", keys);
        Assert.Equal("Retail", result.PrimaryMenuPersona);
    }

    [Fact]
    public async Task Skips_widgets_with_unregistered_provider()
    {
        await using var ctx = CreateContext();
        ctx.DashboardWidget.Add(Widget("bad", "Retail", "unknown_provider"));
        ctx.DashboardWidget.Add(Widget("good", "Retail", "operations_today"));
        await ctx.SaveChangesAsync();

        var handler = new GetMyDashboardWidgetsHandler(
            new TestCatalogReader(ctx),
            new DashboardWidgetRegistry([new StubProvider("operations_today")]),
            new NullOperatorContext());

        var result = await handler.Handle(
            new GetMyDashboardWidgetsRequest { Roles = [TelecomRoles.Showroom] },
            CancellationToken.None);

        Assert.Single(result.Data!);
        Assert.Equal("good", result.Data![0].WidgetKey);
    }

    private static QueryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options);
    }

    private static DashboardWidget Widget(
        string key,
        string personas,
        string? provider,
        DashboardWidgetKind kind = DashboardWidgetKind.Stat) =>
        new()
        {
            WidgetKey = key,
            TitleAr = key,
            PersonasAllowed = personas,
            ProviderKey = provider,
            WidgetKind = kind,
            GridSize = DashboardWidgetGridSize.Medium,
            IsActive = true,
        };

    private sealed class TestCatalogReader(QueryContext ctx) : IDashboardWidgetCatalogReader
    {
        public async Task<IReadOnlyList<DashboardWidget>> GetActiveWidgetsAsync(CancellationToken cancellationToken = default) =>
            await ctx.DashboardWidget.AsNoTracking().Where(w => w.IsActive).ToListAsync(cancellationToken);

        public void InvalidateCache() { }
    }

    private sealed class NullOperatorContext : IOperatorContext
    {
        public string? UserId => null;
        public IReadOnlyList<string> Roles => [];
        public TelecomMenuPersona? EffectivePersona => null;
        public bool IsAuthenticated => false;
    }

    private sealed class StubProvider(string key) : IDashboardWidgetDataProvider
    {
        public string ProviderKey => key;

        public Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DashboardWidgetDataDto { ProviderKey = key, ValueText = "1" });
    }
}
