using Application.Common;

namespace Application.Common.Exceptions;

/// <summary>Domain / business rule failure intended for HTTP 400 with a user-facing message.</summary>
public class BusinessRuleViolationException : Exception
{
    public string MessageAr { get; }
    public string MessageEn { get; }

    public BusinessRuleViolationException(string messageAr, string? messageEn = null) : base(messageAr)
    {
        MessageAr = messageAr;
        MessageEn = string.IsNullOrWhiteSpace(messageEn) ? messageAr : messageEn!;
    }

    public BusinessRuleViolationException(BilingualUserMessage message)
        : this(message.Ar, message.En)
    {
    }
}
