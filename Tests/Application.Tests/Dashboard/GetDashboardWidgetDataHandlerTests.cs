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

public class GetDashboardWidgetDataHandlerTests
{
    public GetDashboardWidgetDataHandlerTests() => DashboardTestEncryption.EnsureInitialized();

    [Fact]
    public async Task Executive_can_load_provider_shared_with_call_center_widget()
    {
        await using var ctx = CreateContext();
        ctx.DashboardWidget.AddRange(
            Widget("exec_network", "Executive", "network_pulse"),
            Widget("cc_network", "CallCenter", "network_pulse"));
        await ctx.SaveChangesAsync();

        var handler = new GetDashboardWidgetDataHandler(
            new TestCatalogReader(ctx),
            new DashboardWidgetRegistry([new StubProvider("network_pulse")]),
            new StubOperatorContext());

        var result = await handler.Handle(
            new GetDashboardWidgetDataRequest
            {
                Roles = [TelecomRoles.Management],
                ProviderKey = "network_pulse",
            },
            CancellationToken.None);

        Assert.Equal("network_pulse", result.Data!.ProviderKey);
    }

    private static QueryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options);
    }

    private static DashboardWidget Widget(string key, string personas, string provider) =>
        new()
        {
            WidgetKey = key,
            TitleAr = key,
            PersonasAllowed = personas,
            ProviderKey = provider,
            WidgetKind = DashboardWidgetKind.Stat,
            GridSize = DashboardWidgetGridSize.Medium,
            IsActive = true,
        };

    private sealed class TestCatalogReader(QueryContext ctx) : IDashboardWidgetCatalogReader
    {
        public async Task<IReadOnlyList<DashboardWidget>> GetActiveWidgetsAsync(
            CancellationToken cancellationToken = default) =>
            await ctx.DashboardWidget.AsNoTracking().Where(w => w.IsActive).ToListAsync(cancellationToken);

        public void InvalidateCache() { }
    }

    private sealed class StubProvider(string key) : IDashboardWidgetDataProvider
    {
        public string ProviderKey => key;

        public Task<DashboardWidgetDataDto> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DashboardWidgetDataDto { ProviderKey = key, ValueText = "ok" });
    }

    private sealed class StubOperatorContext : IOperatorContext
    {
        public string? UserId => null;
        public IReadOnlyList<string> Roles { get; } = [];
        public TelecomMenuPersona? EffectivePersona => TelecomMenuPersona.Executive;
        public bool IsAuthenticated => true;
    }
}
