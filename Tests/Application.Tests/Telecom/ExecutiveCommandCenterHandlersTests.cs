using Application.Common.Audit;
using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Application.Tests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class ExecutiveCommandCenterHandlersTests
{
    [Fact]
    public async Task OperationsSnapshot_returns_pending_and_payment_kpis()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomOperationRequest.Add(new TelecomOperationRequest
        {
            Id = "op-pending-1",
            Number = "OP-001",
            Kind = TelecomOperationKind.NewActivation,
            Status = TelecomOperationStatus.PendingDocuments,
            SubscriberProfileId = "sp-1",
            BranchId = TestOperatorContext.DefaultBranchId,
            CreatedAtUtc = now,
            IsDeleted = false,
        });
        ctx.TelecomPaymentTransaction.Add(new TelecomPaymentTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Number = "PAY-CC-1",
            Status = PaymentTransactionStatus.Completed,
            Amount = 5000m,
            TransactionType = PaymentTransactionType.Recharge,
            PaymentChannel = PaymentChannel.Wallet,
            CustomerId = "cust-1",
            SubscriberProfileId = "sp-1",
            BranchId = TestOperatorContext.DefaultBranchId,
            CreatedAtUtc = now,
            ConfirmedAtUtc = now.AddMinutes(1),
            IsDeleted = false,
        });
        await ctx.SaveChangesAsync();

        var mediator = new RoutingMediator(ctx);
        var handler = new GetExecutiveOperationsSnapshotHandler(
            ctx,
            StubOperationalAnalyticsScopeService.Instance,
            mediator);

        var result = await handler.Handle(
            new GetExecutiveOperationsSnapshotRequest(null, null, now.Date, now.AddHours(1)),
            CancellationToken.None);

        Assert.Equal(1, result.PendingOperations);
        Assert.Single(result.TopPendingOperations);
        Assert.Equal(5000m, result.PaymentRechargeToday);
        Assert.Equal(1, result.PaymentCompletedToday);
    }

    [Fact]
    public async Task WorkforceReport_scores_employee_from_operations()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        const string employeeId = "emp-showroom-1";

        ctx.TelecomOperationRequest.AddRange(
            Op("OP-A", employeeId, TelecomOperationStatus.Completed, now),
            Op("OP-B", employeeId, TelecomOperationStatus.Confirmed, now));

        await ctx.SaveChangesAsync();

        var scope = new WorkforceAnalyticsScope
        {
            DataScope = new StrategicDataScope
            {
                ScopeLabelAr = "اختبار",
                AccessLevel = StrategicAccessLevel.GeneralManager,
                EffectiveBranchIds = [TestOperatorContext.DefaultBranchId],
            },
            Users =
            [
                new WorkforceUserSnapshot
                {
                    UserId = employeeId,
                    DisplayName = "موظف معرض",
                    OrgUnitNameAr = "فرع المزة",
                    Roles = ["TelecomShowroom"],
                    IsOnline = true,
                },
            ],
            AllowedUserIds = [employeeId],
        };

        var handler = new GetWorkforcePerformanceReportHandler(
            ctx,
            new FixedWorkforceScope(scope),
            new EmptyAuditReadService(),
            TestOperatorContext.Instance,
            new NoOpAuditService());

        var result = await handler.Handle(
            new GetWorkforcePerformanceReportRequest(null, null, now.AddDays(-7), now, WorkforceRoleFilter.Showroom),
            CancellationToken.None);

        Assert.Single(result.Rows);
        Assert.Equal(2, result.Rows[0].OperationsCreated);
        Assert.True(result.Rows[0].ProductivityScore > 0);
    }

    [Fact]
    public async Task EmployeeDetail_rejects_user_outside_scope()
    {
        var scope = new WorkforceAnalyticsScope
        {
            DataScope = new StrategicDataScope
            {
                ScopeLabelAr = "اختبار",
                EffectiveBranchIds = [TestOperatorContext.DefaultBranchId],
            },
            Users = [],
            AllowedUserIds = ["allowed-only"],
        };

        var handler = new GetEmployeePerformanceDetailHandler(
            CreateContext(),
            new FixedWorkforceScope(scope),
            new EmptyAuditReadService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(
                new GetEmployeePerformanceDetailRequest("other-user", null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task StrategicMetrics_includes_churn60()
    {
        await using var ctx = CreateContext();
        var handler = new GetStrategicMetricsHandler(
            ctx,
            StubOperationalAnalyticsScopeService.Instance,
            new StubExecutiveFinancialMetricsWithChurn60(),
            TestOperatorContext.Instance,
            new NoOpAuditService());

        var result = await handler.Handle(new GetStrategicMetricsRequest(null, null), CancellationToken.None);

        Assert.Equal(4.5m, result.ChurnPercent60);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private static TelecomOperationRequest Op(
        string number,
        string createdBy,
        TelecomOperationStatus status,
        DateTime created) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Kind = TelecomOperationKind.NewActivation,
            Status = status,
            SubscriberProfileId = "sp-1",
            CreatedById = createdBy,
            BranchId = TestOperatorContext.DefaultBranchId,
            CreatedAtUtc = created,
            IsDeleted = false,
        };

    private sealed class RoutingMediator(QueryContext ctx) : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is GetPaymentServicesKpisRequest payReq)
            {
                return (Task<TResponse>)(object)new GetPaymentServicesKpisHandler(ctx, StubOperationalAnalyticsScopeService.Instance)
                    .Handle(payReq, cancellationToken);
            }

            if (request is GetSellingLineActivationKpisRequest actReq)
            {
                return (Task<TResponse>)(object)new GetSellingLineActivationKpisHandler(ctx, StubOperationalAnalyticsScopeService.Instance)
                    .Handle(actReq, cancellationToken);
            }

            throw new NotSupportedException(request.GetType().Name);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new NotImplementedException();

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FixedWorkforceScope(WorkforceAnalyticsScope scope) : IWorkforceAnalyticsScopeService
    {
        public Task<WorkforceAnalyticsScope> ResolveAsync(
            string? requestedRegionId,
            string? requestedBranchId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(scope);

        public Task<bool> CanViewUserAsync(string targetUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(scope.AllowedUserIds.Contains(targetUserId));
    }

    private sealed class EmptyAuditReadService : IUserAuditReadService
    {
        public Task<UserAuditLogQueryResult> QueryAsync(UserAuditLogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new UserAuditLogQueryResult());

        public Task<int> CountAsync(UserAuditLogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class NoOpAuditService : IUserAuditService
    {
        public Task LogAsync(UserAuditLogRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class StubExecutiveFinancialMetricsWithChurn60 : IExecutiveFinancialMetricsService
    {
        public Task<ExecutiveFinancialMetrics> ComputeAsync(
            IReadOnlyList<string> branchIds,
            DateTime periodToUtc,
            int billingCycleDays = 30,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ExecutiveFinancialMetrics
            {
                ChurnPercent30 = 2m,
                ChurnPercent60 = 4.5m,
                TotalRevenue = 100m,
            });
    }
}
