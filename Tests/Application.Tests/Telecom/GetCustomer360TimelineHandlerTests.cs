using Application.Common.Audit;
using Application.Features.CustomerManager.Queries;
using Application.Tests.Dashboard;
using Application.Tests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

public class GetCustomer360TimelineHandlerTests
{
    [Fact]
    public async Task Handle_merges_payments_and_supports_kind_filter()
    {
        await using var ctx = CreateContext();
        var customerId = "cust-tl-1";
        ctx.TelecomPaymentTransaction.Add(new TelecomPaymentTransaction
        {
            Id = Guid.NewGuid().ToString(),
            Number = "PAY-TL-1",
            CustomerId = customerId,
            SubscriberProfileId = "prof-tl-1",
            Amount = 5000m,
            Status = PaymentTransactionStatus.Completed,
            TransactionType = PaymentTransactionType.Recharge,
            PaymentChannel = PaymentChannel.Wallet,
            Msisdn = "0939000999",
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false,
        });
        await ctx.SaveChangesAsync();

        var handler = new GetCustomer360TimelineHandler(ctx, new StubAuditRead());

        var all = await handler.Handle(
            new GetCustomer360TimelineRequest { CustomerId = customerId, Take = 10 },
            CancellationToken.None);
        Assert.Single(all.Items);
        Assert.Equal(Customer360TimelineKind.Payment, all.Items[0].Kind);

        var filtered = await handler.Handle(
            new GetCustomer360TimelineRequest { CustomerId = customerId, Take = 10, Kinds = "2" },
            CancellationToken.None);
        Assert.Empty(filtered.Items);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private sealed class StubAuditRead : IUserAuditReadService
    {
        public Task<UserAuditLogQueryResult> QueryAsync(UserAuditLogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(new UserAuditLogQueryResult { Items = [], TotalCount = 0 });

        public Task<int> CountAsync(UserAuditLogQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
