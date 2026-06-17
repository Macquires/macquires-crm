namespace Application.Common.Exceptions;

public class UnauthorizedPermissionException : Exception
{
    public UnauthorizedPermissionException(
        string message,
        IReadOnlyList<string>? requiredPermissions = null,
        string? endpoint = null) : base(message)
    {
        RequiredPermissions = requiredPermissions ?? [];
        Endpoint = endpoint;
    }

    public IReadOnlyList<string> RequiredPermissions { get; }

    public string? Endpoint { get; }
}
