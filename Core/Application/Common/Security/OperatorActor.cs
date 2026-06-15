using Application.Common.Exceptions;

namespace Application.Common.Security;

public static class OperatorActor
{
    public static string RequireUserId(IOperatorContext context) =>
        string.IsNullOrWhiteSpace(context.UserId)
            ? throw new BusinessRuleViolationException("يجب تسجيل الدخول لتنفيذ هذه العملية.")
            : context.UserId;

    public static string? ResolveBranchId(IOperatorContext context) =>
        string.IsNullOrWhiteSpace(context.BranchId) ? null : context.BranchId.Trim();
}
