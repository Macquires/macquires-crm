using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomPaymentRechargeE2ETests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomPaymentRechargeE2ETests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task WalletRecharge_CreateAndConfirm_CompletesWithBalance()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var client = CreateClient(_fixture);
        client.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(client.Client, ct));

        var seed = await E2ETelecomOperationSeeds.ResolveAsync(
            _fixture.AppFactory.Services,
            TelecomOperationKind.Migration,
            "recharge-wallet-line",
            ct);

        var subscriptionId = await E2ETestDataHelper.ResolveSubscriptionIdForLineAsync(
            _fixture.AppFactory.Services,
            seed.Line.SubscriberProfileId,
            seed.Line.MsisdnAssetId,
            ct);

        var gatewayRef = $"E2E-WAL-{Guid.NewGuid():N}"[..24];
        var draft = await client.CreatePaymentTransactionAsync(
            new
            {
                type = (int)PaymentTransactionType.Recharge,
                customerId = seed.Line.CustomerId,
                subscriptionId,
                amount = 15_000m,
                paymentChannel = (int)PaymentChannel.Wallet,
                serviceChannel = (int)PaymentServiceChannel.Showroom,
            },
            ct);

        Assert.False(string.IsNullOrWhiteSpace(draft.PaymentId));

        var confirm = await client.ConfirmPaymentTransactionAsync(draft.PaymentId, gatewayRef, ct);
        Assert.True(confirm.Success);
        Assert.NotNull(confirm.PaymentNumber);

        using var scope = _fixture.AppFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataContext>();
        var payment = await db.TelecomPaymentTransaction.AsNoTracking()
            .FirstAsync(p => p.Id == draft.PaymentId, ct);
        Assert.Equal(PaymentTransactionStatus.Completed, payment.Status);
        Assert.Equal(gatewayRef, payment.GatewayReference);
    }
}
