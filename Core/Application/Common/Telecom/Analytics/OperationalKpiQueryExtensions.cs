using Domain.Entities;

namespace Application.Common.Telecom.Analytics;

public static class OperationalKpiQueryExtensions
{
    public static IQueryable<TelecomOperationRequest> InBranchScope(
        this IQueryable<TelecomOperationRequest> query,
        IReadOnlyList<string> branchIds) =>
        branchIds.Count == 0
            ? query
            : query.Where(o => o.BranchId == null || branchIds.Contains(o.BranchId!));

    public static IQueryable<TelecomPaymentTransaction> InBranchScope(
        this IQueryable<TelecomPaymentTransaction> query,
        IReadOnlyList<string> branchIds) =>
        branchIds.Count == 0
            ? query
            : query.Where(p => p.BranchId == null || branchIds.Contains(p.BranchId!));
}
