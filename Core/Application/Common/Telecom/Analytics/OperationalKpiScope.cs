using Application.Common.Security;

namespace Application.Common.Telecom.Analytics;

/// <summary>Shared branch/regional scope parameters for operational KPI endpoints.</summary>
public interface IOperationalKpiScopeRequest
{
    string? RegionId { get; }
    string? BranchId { get; }
}

/// <summary>MIS KPI queries — scoped analytics with <see cref="PermissionCatalog.TelecomReportsMis"/>.</summary>
public interface IOperationalKpiRequest : IOperationalKpiScopeRequest;