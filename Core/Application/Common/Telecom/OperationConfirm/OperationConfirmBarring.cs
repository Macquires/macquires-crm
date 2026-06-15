using Application.Common.Telecom.Suspension;
using Domain.Entities;

namespace Application.Common.Telecom.OperationConfirm;

internal static class OperationConfirmBarring
{
    public static void ApplyToProfile(SubscriberProfile profile, string barringLevel, string msisdn)
    {
        if (string.Equals(barringLevel, SuspensionWellKnown.BarringInboundOnly, StringComparison.OrdinalIgnoreCase))
        {
            profile.SuspendInbound(msisdn);
        }
        else if (string.Equals(barringLevel, SuspensionWellKnown.BarringOutboundOnly, StringComparison.OrdinalIgnoreCase))
        {
            profile.SuspendOutbound(msisdn);
        }
        else if (string.Equals(barringLevel, SuspensionWellKnown.BarringDataOnly, StringComparison.OrdinalIgnoreCase))
        {
            // Voice/SMS remain active; data context is barred at HLR/CBS.
        }
        else
        {
            profile.Suspend(msisdn);
        }
    }
}
