using Application.Common.Telecom.Refund;
using Application.Features.TelecomManager.Queries;
using Application.Tests.Dashboard;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Telecom;

using Application.Tests.TestSupport;

public class GetRefundKpisHandlerTests
{
    [Fact]
    public async Task Handle_aggregates_refund_operations_for_today()
    {
        await using var ctx = CreateContext();
        var now = DateTime.UtcNow;
        ctx.TelecomOperationRequest.AddRange(
            Op("RFD-1", TelecomOperationStatus.Completed, RefundWellKnown.TypeDeposit, 5000m, RefundWellKnown.SettlementSettled, now, false),
            Op("RFD-2", TelecomOperationStatus.Failed, RefundWellKnown.TypeSyriatelCash, 100m, null, now, true),
            Op("RFD-3", TelecomOperationStatus.PendingDocuments, RefundWellKnown.TypeWalletBalance, 200m, null, now, false, "BackOffice"));
        await ctx.SaveChangesAsync();

        var handler = new GetRefundKpisHandler(ctx);
        var result = await handler.Handle(
            new GetRefundKpisRequest { FromUtc = now.Date, ToUtc = now.AddHours(1) },
            CancellationToken.None);

        Assert.Equal(3, result.TotalToday);
        Assert.Equal(1, result.CompletedToday);
        Assert.Equal(1, result.FailedToday);
        Assert.Equal(1, result.PendingBackOffice);
        Assert.Equal(5000m, result.SettledAmountToday);
        Assert.Equal(1, result.DualApprovalToday);
        Assert.Single(result.RejectionReasons);
    }

    private static QueryContext CreateContext()
    {
        DashboardTestEncryption.EnsureInitialized();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options, TestOperatorContext.Instance);
    }

    private static TelecomOperationRequest Op(
        string number,
        TelecomOperationStatus status,
        string refundType,
        decimal amount,
        string? settlement,
        DateTime created,
        bool dualApproval,
        string? approval = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Number = number,
            Kind = TelecomOperationKind.DepositRefundSettlement,
            Status = status,
            RefundType = refundType,
            RefundReason = status == TelecomOperationStatus.Failed ? "Rejected-KYC" : "OK",
            RefundAmount = amount,
            RefundSettlementStatus = settlement,
            RequiresDualApproval = dualApproval,
            CreatedAtUtc = created,
            ConfirmedAtUtc = status == TelecomOperationStatus.Completed ? created.AddMinutes(10) : null,
            ApprovalLevelRequired = approval,
            SubscriberProfileId = "prof-rfd",
            IsDeleted = false,
        };
}
