using Application.Common.Telecom.BadDebt;
using Application.Common.Telecom.ChangeGsm;
using Application.Common.Telecom.ChangeNumber;
using Application.Common.Telecom.DeviceSales;
using Application.Common.Telecom.OfferSubscription;
using Application.Common.Telecom.Reconnect;
using Application.Common.Telecom.Refund;
using Application.Common.Telecom.SimSwap;
using Application.Common.Telecom.Suspension;
using Application.Common.Telecom.TakeOver;
using Application.Common.Telecom.Termination;
using Application.Features.TelecomManager.Commands;

namespace Application.Common.Telecom.OperationCreate;

public sealed class OperationCreateContext
{
    public OperationCreateContext(CreateTelecomOperationRequest request, string actorUserId)
    {
        Request = request;
        ActorUserId = actorUserId;
    }

    public CreateTelecomOperationRequest Request { get; }
    public string ActorUserId { get; }

    public string? ResolvedOfferingId { get; set; }
    public string? ResolvedProductId { get; set; }

    public TakeOverEligibilityResult? TakeOverEligibility { get; set; }
    public SimSwapEligibilityResult? SimSwapEligibility { get; set; }
    public ChangeNumberEligibilityResult? ChangeNumberEligibility { get; set; }
    public TerminationEligibilityResult? TerminationEligibility { get; set; }
    public SuspensionEligibilityResult? SuspensionEligibility { get; set; }
    public ReconnectEligibilityResult? ReconnectEligibility { get; set; }
    public DeviceSalesEligibilityResult? DeviceSalesEligibility { get; set; }
    public BadDebtEligibilityResult? BadDebtEligibility { get; set; }
    public RefundEligibilityResult? RefundEligibility { get; set; }
    public ChangeGsmEligibilityResult? ChangeGsmEligibility { get; set; }
    public OfferSubscriptionEligibilityResult? MigrationEligibility { get; set; }
}
