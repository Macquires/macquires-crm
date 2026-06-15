using Application.Features.TelecomManager.Commands;
using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

using ASPNET.BackEnd.Common.Base;
using MediatR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/Telecom")]
public class TelecomPaymentsController : BaseApiController
{
    public TelecomPaymentsController(ISender sender) : base(sender) { }

    [RequireTelecomPayment]
    [HttpPost("ValidateVoucher")]
    public async Task<ActionResult<ApiSuccessResult<ValidateVoucherResult>>> ValidateVoucherAsync(
        ValidateVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ValidateVoucherResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ValidateVoucherAsync),
            Content = response
        });
    }

    [RequireTelecomPayment]
    [EnableRateLimiting("telecom-financial")]
    [HttpPost("CreatePaymentTransaction")]
    public async Task<ActionResult<ApiSuccessResult<CreatePaymentTransactionResult>>> CreatePaymentTransactionAsync(
        CreatePaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreatePaymentTransactionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreatePaymentTransactionAsync),
            Content = response
        });
    }

    [RequireTelecomPayment]
    [EnableRateLimiting("telecom-financial")]
    [HttpPost("ConfirmPaymentTransaction")]
    public async Task<ActionResult<ApiSuccessResult<ConfirmPaymentTransactionResult>>> ConfirmPaymentTransactionAsync(
        ConfirmPaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ConfirmPaymentTransactionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ConfirmPaymentTransactionAsync),
            Content = response
        });
    }

    [RequireTelecomPaymentReverse]
    [HttpPost("ReversePaymentTransaction")]
    public async Task<ActionResult<ApiSuccessResult<ReversePaymentTransactionResult>>> ReversePaymentTransactionAsync(
        ReversePaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ReversePaymentTransactionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ReversePaymentTransactionAsync),
            Content = response
        });
    }

    [RequireTelecomRead]
    [HttpGet("GetPaymentTransactionList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentTransactionListResult>>> GetPaymentTransactionListAsync(
        [FromQuery] int take = 30,
        CancellationToken cancellationToken = default)
    {
        var response = await _sender.Send(new GetPaymentTransactionListRequest { Take = take }, cancellationToken);
        return Ok(new ApiSuccessResult<GetPaymentTransactionListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetPaymentTransactionListAsync),
            Content = response
        });
    }

    [RequireTelecomRead]
    [HttpGet("GetPaymentServicesKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentServicesKpisResult>>> GetPaymentServicesKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetPaymentServicesKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, RegionId = regionId, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetPaymentServicesKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetPaymentServicesKpisAsync),
            Content = response
        });
    }

    [RequireTelecomRead]
    [HttpGet("GetPaymentTransactionDetail")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentTransactionDetailResult>>> GetPaymentTransactionDetailAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetPaymentTransactionDetailRequest { Id = id ?? "" }, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(new ApiSuccessResult<GetPaymentTransactionDetailResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetPaymentTransactionDetailAsync),
            Content = response
        });
    }

}
