using System;

namespace Application.Common.Exceptions;

public class UnauthorizedPermissionException : Exception
{
    public UnauthorizedPermissionException(string message) : base(message)
    {
    }
}
