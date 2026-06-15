using Application.Common.Integrations;
using Domain.Entities;

namespace Application.Common.Telecom;

public sealed record HlrFailureCompensationResult(
    bool CbsReversed,
    bool InReversed,
    bool LocalBindCompensated,
    string MessageAr);

/// <summary>Reverses CBS + local bind when HLR hard-fails after CBS succeeded (Ghost Profile mitigation).</summary>
public interface ITelecomHlrFailureCompensator
{
    Task<HlrFailureCompensationResult> CompensateAsync(
        TelecomOperationRequest operation,
        TelecomLineProvisionContext lineContext,
        string? actorUserId,
        string hlrErrorMessage,
        CancellationToken cancellationToken);
}
