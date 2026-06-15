using Application.Common.Integrations;
using Application.Features.TelecomManager.Commands;
using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using HlrLiveStatusResult = Application.Common.Integrations.HlrLiveStatusResult;

using ASPNET.BackEnd.Common.Base;
using MediatR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/Telecom")]
public class TelecomNetworkController : BaseApiController
{
    public TelecomNetworkController(ISender sender) : base(sender) { }

    [RequireTelecomRead]
    [HttpGet("QueryHlrLiveStatus")]
    public async Task<ActionResult<ApiSuccessResult<HlrLiveStatusResult>>> QueryHlrLiveStatusAsync(
        [FromQuery] string? subscriberProfileId,
        [FromQuery] string? msisdn,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new QueryHlrLiveStatusRequest { SubscriberProfileId = subscriberProfileId ?? "", Msisdn = msisdn },
            cancellationToken);
        return Ok(new ApiSuccessResult<HlrLiveStatusResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(QueryHlrLiveStatusAsync),
            Content = response
        });
    }

    [RequireTelecomRead]
    [HttpGet("CheckHlrStatus")]
    public Task<ActionResult<ApiSuccessResult<HlrLiveStatusResult>>> CheckHlrStatusAsync(
        [FromQuery] string? msisdn,
        [FromQuery] string? subscriberProfileId,
        CancellationToken cancellationToken) =>
        QueryHlrLiveStatusAsync(subscriberProfileId, msisdn, cancellationToken);

    [RequireTelecomConfirm]
    [HttpPost("ReprovisionSubscriberToHlr")]
    public async Task<ActionResult<ApiSuccessResult<HlrReprovisionResult>>> ReprovisionSubscriberToHlrAsync(
        ReprovisionSubscriberToHlrRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<HlrReprovisionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ReprovisionSubscriberToHlrAsync),
            Content = response,
        });
    }

    [RequireTelecomConfirm]
    [HttpPost("ResyncSubscriberFromHlr")]
    public async Task<ActionResult<ApiSuccessResult<HlrResyncResult>>> ResyncSubscriberFromHlrAsync(
        ResyncSubscriberFromHlrRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<HlrResyncResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ResyncSubscriberFromHlrAsync),
            Content = response
        });
    }

    [RequireTelecomInventoryManage]
    [HttpPost("ImportSimInventoryBatch")]
    public async Task<ActionResult<ApiSuccessResult<ImportSimInventoryBatchResult>>> ImportSimInventoryBatchAsync(
        ImportSimInventoryBatchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ImportSimInventoryBatchResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ImportSimInventoryBatchAsync),
            Content = response
        });
    }

    [RequireTelecomProvisioning]
    [HttpPost("UpdateCustomerPrimaryTelecomLine")]
    public async Task<ActionResult<ApiSuccessResult<UpdateCustomerPrimaryTelecomLineResult>>> UpdateCustomerPrimaryTelecomLineAsync(
        UpdateCustomerPrimaryTelecomLineRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateCustomerPrimaryTelecomLineResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UpdateCustomerPrimaryTelecomLineAsync),
            Content = response
        });
    }

    /// <summary>Creates a new <see cref="SubscriberProfile"/> plus first MSISDN line for an existing CRM customer (multi-profile / onboarding).</summary>
    [RequireTelecomProvisioning]
    [HttpPost("RegisterSubscriberProfileForCustomer")]
    public async Task<ActionResult<ApiSuccessResult<RegisterSubscriberProfileForCustomerResult>>> RegisterSubscriberProfileForCustomerAsync(
        RegisterSubscriberProfileForCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<RegisterSubscriberProfileForCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(RegisterSubscriberProfileForCustomerAsync),
            Content = response
        });
    }

    /// <summary>Ensures a <see cref="SubscriberProfile"/> exists for POS onboarding before selling-line activation.</summary>
    [RequireTelecomProvisioning]
    [HttpPost("EnsureSubscriberProfileForCustomer")]
    public async Task<ActionResult<ApiSuccessResult<EnsureSubscriberProfileForCustomerResult>>> EnsureSubscriberProfileForCustomerAsync(
        EnsureSubscriberProfileForCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<EnsureSubscriberProfileForCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(EnsureSubscriberProfileForCustomerAsync),
            Content = response
        });
    }
}
