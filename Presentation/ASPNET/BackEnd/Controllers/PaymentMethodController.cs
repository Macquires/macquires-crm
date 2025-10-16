using Application.Features.PaymentMethodManager.Commands;
using Application.Features.PaymentMethodManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PaymentMethodController : BaseApiController
{
    public PaymentMethodController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreatePaymentMethod")]
    public async Task<ActionResult<ApiSuccessResult<CreatePaymentMethodResult>>> CreatePaymentMethodAsync(CreatePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreatePaymentMethodResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreatePaymentMethodAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdatePaymentMethod")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePaymentMethodResult>>> UpdatePaymentMethodAsync(UpdatePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePaymentMethodResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePaymentMethodAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeletePaymentMethod")]
    public async Task<ActionResult<ApiSuccessResult<DeletePaymentMethodResult>>> DeletePaymentMethodAsync(DeletePaymentMethodRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeletePaymentMethodResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeletePaymentMethodAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPaymentMethodList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentMethodListResult>>> GetPaymentMethodListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetPaymentMethodListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentMethodListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentMethodListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPaymentMethodSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentMethodSingleResult>>> GetPaymentMethodSingleAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetPaymentMethodSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentMethodSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentMethodSingleAsync)}",
            Content = response
        });
    }


}


