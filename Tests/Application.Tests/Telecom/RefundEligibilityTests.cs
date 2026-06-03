using Application.Common.Telecom.Refund;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class RefundEligibilityTests
{
    [Theory]
    [InlineData(RefundWellKnown.TypeDeposit, 10_000, 50_000, 10_000)]
    [InlineData(RefundWellKnown.TypeWalletBalance, 10_000, 50_000, 50_000)]
    [InlineData(RefundWellKnown.TypeOverpayment, 30_000, 50_000, 30_000)]
    public void MaxRefundable_respects_snapshot_caps(string type, decimal deposit, decimal wallet, decimal expected)
    {
        var max = RefundWellKnown.MaxRefundableForType(type, deposit, wallet);
        Assert.Equal(expected, max);
    }

    [Fact]
    public void SyriatelCash_always_requires_back_office()
    {
        Assert.True(RefundWellKnown.RequiresBackOfficeApproval(RefundWellKnown.TypeSyriatelCash, 1_000m, RefundWellKnown.MethodWalletCredit));
    }

    [Fact]
    public void Amount_above_threshold_requires_dual_approval_path()
    {
        Assert.True(RefundWellKnown.RequiresBackOfficeApproval(RefundWellKnown.TypeDeposit, 600_000m, RefundWellKnown.MethodBankTransfer));
    }
}
