using Application.Common.Security;
using Application.Common.Telecom.Analytics;

namespace Application.Tests.TestSupport;

/// <summary>Resolves KPI queries to the default in-memory test branch.</summary>
public sealed class StubOperationalAnalyticsScopeService : IOperationalAnalyticsScopeService
{
    public static StubOperationalAnalyticsScopeService Instance { get; } = new();

    public Task<StrategicDataScope> ResolveScopeAsync(
        string? requestedRegionId,
        string? requestedBranchId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new StrategicDataScope
        {
            AccessLevel = StrategicAccessLevel.GeneralManager,
            CanUseFilters = true,
            ScopeLabelAr = "اختبار",
            EffectiveBranchIds = [TestOperatorContext.DefaultBranchId],
        });
}
