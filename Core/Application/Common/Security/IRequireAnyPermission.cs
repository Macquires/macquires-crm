namespace Application.Common.Security;

/// <summary>MediatR request authorized when the operator has any listed permission.</summary>
public interface IRequireAnyPermission
{
    IReadOnlyList<string> PermissionKeys { get; }
}
