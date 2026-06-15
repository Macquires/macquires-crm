using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using ASPNET.BackEnd.Common.Base;
using MediatR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/Telecom")]
public class TelecomKpisController : BaseApiController
{
    public TelecomKpisController(ISender sender) : base(sender) { }

    [RequireOperationalKpi]
    [HttpGet("GetSellingLineActivationKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetSellingLineActivationKpisResult>>> GetSellingLineActivationKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetSellingLineActivationKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetSellingLineActivationKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetSellingLineActivationKpisAsync),
            Content = response
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetChangeGsmTypeKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetChangeGsmTypeKpisResult>>> GetChangeGsmTypeKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetChangeGsmTypeKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetChangeGsmTypeKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetChangeGsmTypeKpisAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetSuspensionKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetSuspensionKpisResult>>> GetSuspensionKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetSuspensionKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetSuspensionKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetSuspensionKpisAsync),
            Content = response,
        });
    }

    [RequireReconnectEligibility]
    [HttpGet("GetReconnectEligibility")]
    public async Task<ActionResult<ApiSuccessResult<GetReconnectEligibilityResult>>> GetReconnectEligibilityAsync(
        [FromQuery] string subscriberProfileId,
        [FromQuery] string msisdnAssetId,
        CancellationToken cancellationToken,
        [FromQuery] string? reconnectReason = null,
        [FromQuery] string? clearanceType = null,
        [FromQuery] string? paymentReference = null,
        [FromQuery] bool fraudClearanceConfirmed = false,
        [FromQuery] string? sourceSuspensionOperationId = null)
    {
        var response = await _sender.Send(
            new GetReconnectEligibilityRequest
            {
                SubscriberProfileId = subscriberProfileId,
                MsisdnAssetId = msisdnAssetId,
                ReconnectReason = reconnectReason,
                ClearanceType = clearanceType,
                PaymentReference = paymentReference,
                FraudClearanceConfirmed = fraudClearanceConfirmed,
                SourceSuspensionOperationId = sourceSuspensionOperationId,
            },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetReconnectEligibilityResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetReconnectEligibilityAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetReconnectKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetReconnectKpisResult>>> GetReconnectKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetReconnectKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetReconnectKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetReconnectKpisAsync),
            Content = response,
        });
    }

    [RequireBadDebtEligibility]
    [HttpGet("GetBadDebtEligibility")]
    public async Task<ActionResult<ApiSuccessResult<GetBadDebtEligibilityResult>>> GetBadDebtEligibilityAsync(
        [FromQuery] string subscriberProfileId,
        [FromQuery] string msisdnAssetId,
        CancellationToken cancellationToken,
        [FromQuery] string? collectionAction = null,
        [FromQuery] string? dunningStage = null,
        [FromQuery] string? paymentReference = null,
        [FromQuery] decimal? collectedAmount = null,
        [FromQuery] decimal? writeOffAmount = null,
        [FromQuery] bool collectionApprovalConfirmed = false)
    {
        var response = await _sender.Send(
            new GetBadDebtEligibilityRequest
            {
                SubscriberProfileId = subscriberProfileId,
                MsisdnAssetId = msisdnAssetId,
                CollectionAction = collectionAction,
                DunningStage = dunningStage,
                PaymentReference = paymentReference,
                CollectedAmount = collectedAmount,
                WriteOffAmount = writeOffAmount,
                CollectionApprovalConfirmed = collectionApprovalConfirmed,
            },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetBadDebtEligibilityResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetBadDebtEligibilityAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetBadDebtKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetBadDebtKpisResult>>> GetBadDebtKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetBadDebtKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetBadDebtKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetBadDebtKpisAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetRefundKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetRefundKpisResult>>> GetRefundKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetRefundKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetRefundKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetRefundKpisAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetTerminationKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetTerminationKpisResult>>> GetTerminationKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetTerminationKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetTerminationKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTerminationKpisAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetOfferSubscriptionKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetOfferSubscriptionKpisResult>>> GetOfferSubscriptionKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetOfferSubscriptionKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetOfferSubscriptionKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetOfferSubscriptionKpisAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetChangeNumberKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetChangeNumberKpisResult>>> GetChangeNumberKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetChangeNumberKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetChangeNumberKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetChangeNumberKpisAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetSimSwapKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetSimSwapKpisResult>>> GetSimSwapKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetSimSwapKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetSimSwapKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetSimSwapKpisAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetTakeOverOwnershipKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetTakeOverOwnershipKpisResult>>> GetTakeOverOwnershipKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetTakeOverOwnershipKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetTakeOverOwnershipKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTakeOverOwnershipKpisAsync),
            Content = response,
        });
    }
}
