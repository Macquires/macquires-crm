namespace Application.Common.Security;

/// <summary>MediatR requests that require a granular permission when evaluated.</summary>
public interface IRequirePermission
{
    string PermissionKey { get; }
}
