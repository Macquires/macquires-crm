using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Application.Tests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class GetExecutiveCommandCenterSummaryHandlerTests
{
    [Fact]
    public async Task Handle_returns_scope_metadata_when_branches_empty()
    {
        await using var ctx = CreateContext();
        var scope = new Application.Common.Security.StrategicDataScope
        {
            ScopeLabelAr = "فرع حلب",
            AccessLevel = Application.Common.Security.StrategicAccessLevel.BranchManager,
            CanUseFilters = false,
            EffectiveBranchIds = [],
        };

        var handler = new GetExecutiveCommandCenterSummaryHandler(
            ctx,
            new FixedScopeService(scope),
            new StubExecutiveFinancialMetricsService(),
            new StubWorkforceUserReadService(),
            TestOperatorContext.Instance,
            new NoOpUserAuditService(),
            new StubMediator());

        var result = await handler.Handle(
            new GetExecutiveCommandCenterSummaryRequest(null, null, null, null),
            CancellationToken.None);

        Assert.Equal("فرع حلب", result.ScopeLabelAr);
        Assert.Equal(0, result.OperationsToday);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private sealed class FixedScopeService(Application.Common.Security.StrategicDataScope scope)
        : Application.Common.Telecom.Analytics.IOperationalAnalyticsScopeService
    {
        public Task<Application.Common.Security.StrategicDataScope> ResolveScopeAsync(
            string? requestedRegionId,
            string? requestedBranchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(scope);
    }

    private sealed class StubExecutiveFinancialMetricsService
        : Application.Common.Telecom.Analytics.IExecutiveFinancialMetricsService
    {
        public Task<Application.Common.Telecom.Analytics.ExecutiveFinancialMetrics> ComputeAsync(
            IReadOnlyList<string> branchIds,
            DateTime periodToUtc,
            int billingCycleDays = 30,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Application.Common.Telecom.Analytics.ExecutiveFinancialMetrics
            {
                TotalRevenue = 1000m,
                ChurnPercent30 = 2m,
                ChurnPercent60 = 3m,
            });
    }

    private sealed class StubWorkforceUserReadService : Application.Common.Telecom.Analytics.IWorkforceUserReadService
    {
        public Task<IReadOnlyList<Application.Common.Telecom.Analytics.WorkforceUserSnapshot>> GetUsersInBranchesAsync(
            IReadOnlyList<string> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Application.Common.Telecom.Analytics.WorkforceUserSnapshot>>([]);

        public Task<int> CountOnlineInBranchesAsync(
            IReadOnlyList<string> branchIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class NoOpUserAuditService : Application.Common.Audit.IUserAuditService
    {
        public Task LogAsync(Application.Common.Audit.UserAuditLogRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubMediator : MediatR.IMediator
    {
        public Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            request switch
            {
                GetExecutiveExceptionsRequest => (Task<TResponse>)(object)Task.FromResult(new GetExecutiveExceptionsResult()),
                GetExecutiveWeeklyDigestRequest => (Task<TResponse>)(object)Task.FromResult(new GetExecutiveWeeklyDigestResult { SummaryAr = "ok" }),
                _ => throw new NotSupportedException(request.GetType().Name),
            };

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : MediatR.IRequest =>
            Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : MediatR.INotification =>
            Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            MediatR.IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
