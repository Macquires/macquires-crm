using Application.Features.TelecomManager.Commands;
using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using Microsoft.AspNetCore.Mvc;

using ASPNET.BackEnd.Common.Base;
using MediatR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/Telecom")]
public class TelecomDeviceSalesController : BaseApiController
{
    public TelecomDeviceSalesController(ISender sender) : base(sender) { }

    [RequireTelecomRead]
    [HttpGet("GetDeviceInventoryList")]
    public async Task<ActionResult<ApiSuccessResult<GetDeviceInventoryListResult>>> GetDeviceInventoryListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? status = null,
        [FromQuery] string? imeiContains = null)
    {
        var response = await _sender.Send(
            new GetDeviceInventoryListRequest { Status = status, ImeiContains = imeiContains },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetDeviceInventoryListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetDeviceInventoryListAsync),
            Content = response,
        });
    }

    [RequireTelecomRead]
    [HttpGet("GetInstallmentPlanList")]
    public async Task<ActionResult<ApiSuccessResult<GetInstallmentPlanListResult>>> GetInstallmentPlanListAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetInstallmentPlanListRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetInstallmentPlanListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetInstallmentPlanListAsync),
            Content = response,
        });
    }

    [RequireTelecomCreate]
    [HttpPost("CreateDeviceInventory")]
    public async Task<ActionResult<ApiSuccessResult<CreateDeviceInventoryResult>>> CreateDeviceInventoryAsync(
        CreateDeviceInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateDeviceInventoryResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreateDeviceInventoryAsync),
            Content = response,
        });
    }

    [RequireTelecomCreate]
    [HttpPost("RecordDeviceDownPayment")]
    public async Task<ActionResult<ApiSuccessResult<RecordDeviceDownPaymentResult>>> RecordDeviceDownPaymentAsync(
        RecordDeviceDownPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<RecordDeviceDownPaymentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(RecordDeviceDownPaymentAsync),
            Content = response,
        });
    }

    [RequireTelecomConfirm]
    [HttpPost("ApproveDeviceInstallment")]
    public async Task<ActionResult<ApiSuccessResult<ApproveDeviceInstallmentResult>>> ApproveDeviceInstallmentAsync(
        ApproveDeviceInstallmentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ApproveDeviceInstallmentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ApproveDeviceInstallmentAsync),
            Content = response,
        });
    }

    [RequireTelecomRead]
    [HttpGet("GetDeviceSaleKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetDeviceSaleKpisResult>>> GetDeviceSaleKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetDeviceSaleKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetDeviceSaleKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetDeviceSaleKpisAsync),
            Content = response,
        });
    }
}
