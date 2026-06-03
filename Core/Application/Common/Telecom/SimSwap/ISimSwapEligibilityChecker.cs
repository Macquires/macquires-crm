using Domain.Entities;



namespace Application.Common.Telecom.SimSwap;



public interface ISimSwapEligibilityChecker

{

    Task<SimSwapEligibilityResult> ValidateForCreateAsync(

        string subscriberProfileId,

        string? msisdnAssetId,

        string? simInventoryId,

        string? simIccid,

        string replacementReason,

        bool isLostOrStolenReport,

        string? excludeOperationId = null,

        CancellationToken cancellationToken = default);



    void ValidateDocumentsForConfirm(TelecomOperationRequest operation);

    Task<SimSwapEligibilityResult> ValidateForConfirmAsync(
        TelecomOperationRequest operation,
        CancellationToken cancellationToken = default);
}



public sealed record SimSwapEligibilityResult(

    bool Allowed,

    string MessageAr,

    string? CustomerId,

    string? Msisdn,

    string? PriorSimInventoryId,

    string? NewSimInventoryId,

    string ValidationCode);


