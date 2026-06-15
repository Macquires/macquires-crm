using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class GetPaymentServicesKpisHandlerTests
{
    [Fact]
    public async Task Handle_aggregates_payment_transactions_for_today()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomPaymentTransaction.AddRange(
            Pay("PAY-1", PaymentTransactionStatus.Completed, 10_000m, now, now.AddMinutes(1)),
            Pay("PAY-2", PaymentTransactionStatus.Failed, 500m, now, null),
            Pay("PAY-3", PaymentTransactionStatus.Reversed, 2000m, now, now.AddMinutes(2)));
        await ctx.SaveChangesAsync();

        var handler = new GetPaymentServicesKpisHandler(ctx, StubOperationalAnalyticsScopeService.Instance);
        var result = await handler.Handle(
            new GetPaymentServicesKpisRequest { FromUtc = now.Date, ToUtc = now.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(10_000m, result.TotalRechargedAmountToday);
        Assert.Equal(1, result.CompletedCountToday);
        Assert.Equal(1, result.FailedCountToday);
        Assert.Equal(1, result.ReversedCountToday);
        Assert.Equal(50m, result.FailureRatePercent);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<QueryContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private static TelecomPaymentTransaction Pay(
        string number,
        PaymentTransactionStatus status,
        decimal amount,
        DateTime created,
        DateTime? confirmed) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Status = status,
            Amount = amount,
            TransactionType = PaymentTransactionType.Recharge,
            PaymentChannel = PaymentChannel.Wallet,
            CustomerId = "cust-pay",
            SubscriberProfileId = "prof-pay",
            Msisdn = "0939000100",
            CreatedAtUtc = created,
            ConfirmedAtUtc = confirmed,
            BranchId = TestOperatorContext.DefaultBranchId,
            IsDeleted = false,
        };
}
