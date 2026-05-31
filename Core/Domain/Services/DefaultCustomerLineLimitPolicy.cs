using Domain.Entities;
using Domain.Enums;

namespace Domain.Services;

/// <summary>سياسة افتراضية لعدد الخطوط — يمكن استبدالها لاحقاً من إعدادات الشركة.</summary>
public sealed class DefaultCustomerLineLimitPolicy : ICustomerLineLimitPolicy
{
    private const int DefaultMax = 10;
    private const int CorporateMax = 50;

    public int GetMaxLinesAllowed(Customer customer, int currentActiveLineCount)
    {
        return customer.CustomerKind == CustomerKind.Corporate ? CorporateMax : DefaultMax;
    }
}
