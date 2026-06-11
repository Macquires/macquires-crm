using Application.Common.Exceptions;

namespace Application.Common.Exceptions;

public sealed class TicketAlreadyClaimedException : BusinessRuleViolationException
{
    public TicketAlreadyClaimedException(string messageAr, string? messageEn = null) 
        : base(messageAr, messageEn)
    {
    }
}
