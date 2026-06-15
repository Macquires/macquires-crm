using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public sealed class TelecomOperationSchedulePolicyTests
{
    [Theory]
    [InlineData(TelecomOperationKind.Migration, true)]
    [InlineData(TelecomOperationKind.ChangeGsmType, true)]
    [InlineData(TelecomOperationKind.TakeOver, true)]
    [InlineData(TelecomOperationKind.Termination, true)]
    [InlineData(TelecomOperationKind.TemporarySuspension, true)]
    [InlineData(TelecomOperationKind.NumberPortability, true)]
    [InlineData(TelecomOperationKind.SimSwap, true)]
    [InlineData(TelecomOperationKind.NewActivation, true)]
    [InlineData(TelecomOperationKind.Reconnect, true)]
    [InlineData(TelecomOperationKind.DepositRefundSettlement, true)]
    [InlineData(TelecomOperationKind.BadDebtRecovery, true)]
    [InlineData(TelecomOperationKind.DeviceSale, true)]
    public void SupportsScheduling_matches_kind(TelecomOperationKind kind, bool expected) =>
        Assert.Equal(expected, TelecomOperationSchedulePolicy.SupportsScheduling(kind));

    [Fact]
    public void ShouldDeferToScheduled_when_migration_date_in_future()
    {
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var op = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.Migration,
            MigrationEffectiveDateUtc = now.AddDays(7),
        };

        Assert.True(TelecomOperationSchedulePolicy.ShouldDeferToScheduled(op, now));
    }

    [Fact]
    public void ShouldDeferToScheduled_when_activation_date_in_future()
    {
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var op = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.NewActivation,
            ActivationEffectiveDateUtc = now.AddDays(3),
        };

        Assert.True(TelecomOperationSchedulePolicy.ShouldDeferToScheduled(op, now));
    }

    [Fact]
    public void ShouldDeferToScheduled_false_when_within_grace_window()
    {
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var op = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.Migration,
            MigrationEffectiveDateUtc = now.AddSeconds(30),
        };

        Assert.False(TelecomOperationSchedulePolicy.ShouldDeferToScheduled(op, now));
    }

    [Fact]
    public void IsDueForExecution_when_scheduled_and_past_effective_date()
    {
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var op = new TelecomOperationRequest
        {
            Kind = TelecomOperationKind.Migration,
            Status = TelecomOperationStatus.Scheduled,
            MigrationEffectiveDateUtc = now.AddHours(-1),
        };

        Assert.True(TelecomOperationSchedulePolicy.IsDueForExecution(op, now));
    }

    [Fact]
    public void Lifecycle_allows_confirmed_to_scheduled_and_scheduled_to_provisioning()
    {
        Assert.True(TelecomOperationLifecycle.CanTransition(
            TelecomOperationStatus.Confirmed,
            TelecomOperationStatus.Scheduled));
        Assert.True(TelecomOperationLifecycle.CanTransition(
            TelecomOperationStatus.Scheduled,
            TelecomOperationStatus.Provisioning));
    }
}
