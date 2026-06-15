using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Application.Tests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class GetExecutiveExceptionsHandlerTests
{
    [Fact]
    public async Task Handle_flags_low_integration_health()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            ctx.BillingIntegrationLog.Add(new BillingIntegrationLog
            {
                Id = Guid.NewGuid().ToString(),
                IntegrationTarget = "CBS",
                Success = i < 2,
                Message = i < 2 ? "ok" : "fail",
                CreatedAtUtc = now.AddHours(-1),
                IsDeleted = false,
            });
        }
        await ctx.SaveChangesAsync();

        var handler = new GetExecutiveExceptionsHandler(
            ctx,
            StubOperationalAnalyticsScopeService.Instance,
            new StubExecutiveFinancialMetricsService());

        var result = await handler.Handle(new GetExecutiveExceptionsRequest(null, null), CancellationToken.None);

        Assert.Contains(result.Items, i => i.Code == "integration_health_low");
        Assert.True(result.WarningCount + result.CriticalCount > 0);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private sealed class StubExecutiveFinancialMetricsService : Application.Common.Telecom.Analytics.IExecutiveFinancialMetricsService
    {
        public Task<Application.Common.Telecom.Analytics.ExecutiveFinancialMetrics> ComputeAsync(
            IReadOnlyList<string> branchIds,
            DateTime periodToUtc,
            int billingCycleDays = 30,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Application.Common.Telecom.Analytics.ExecutiveFinancialMetrics());
    }
}
