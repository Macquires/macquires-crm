using Domain.Enums;
using Infrastructure.TelecomIntegrations;
using Xunit;

namespace Application.Tests.Telecom;

public class CashierSystemMockIntegrationTests
{
    [Fact]
    public async Task FetchPayment_REC_prefix_returns_expected_amount()
    {
        var integration = new CashierSystemMockIntegration();
        var result = await integration.FetchPaymentByReferenceAsync("REC-2026-ACT", 50_000m);

        Assert.True(result.Success);
        Assert.Equal(50_000m, result.AmountPaid);
        Assert.Equal(PaymentChannel.Cash, result.PaymentChannel);
        Assert.Equal("REC-2026-ACT", result.PaymentReference);
    }

    [Fact]
    public async Task FetchPayment_non_REC_prefix_fails()
    {
        var integration = new CashierSystemMockIntegration();
        var result = await integration.FetchPaymentByReferenceAsync("PAY-123", 10_000m);

        Assert.False(result.Success);
        Assert.Null(result.AmountPaid);
    }

    [Fact]
    public async Task FetchPayment_without_expected_uses_demo_default()
    {
        var integration = new CashierSystemMockIntegration();
        var result = await integration.FetchPaymentByReferenceAsync("REC-DEMO", 0m);

        Assert.True(result.Success);
        Assert.Equal(50_000m, result.AmountPaid);
    }
}
