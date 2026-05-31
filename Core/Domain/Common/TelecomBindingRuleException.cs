namespace Domain.Common;

/// <summary>انتهاك قاعدة ربط ثلاثي (عميل + رقم + شريحة) في Domain.</summary>
public sealed class TelecomBindingRuleException : Exception
{
    public string Code { get; }

    public TelecomBindingRuleException(string code, string message) : base(message)
    {
        Code = code;
    }
}
