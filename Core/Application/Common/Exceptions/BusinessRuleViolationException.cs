namespace Application.Common.Exceptions;

/// <summary>Domain / business rule failure intended for HTTP 400 with a user-facing message.</summary>
public sealed class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException(string message) : base(message)
    {
    }
}
