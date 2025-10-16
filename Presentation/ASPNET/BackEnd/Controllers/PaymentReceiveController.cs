using Application.Features.PaymentReceiveManager.Commands;
using Application.Features.PaymentReceiveManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PaymentReceiveController : BaseApiController
{
    public PaymentReceiveController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreatePaymentReceive")]
    public async Task<ActionResult<ApiSuccessResult<CreatePaymentReceiveResult>>> CreatePaymentReceiveAsync(CreatePaymentReceiveRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreatePaymentReceiveResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreatePaymentReceiveAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdatePaymentReceive")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePaymentReceiveResult>>> UpdatePaymentReceiveAsync(UpdatePaymentReceiveRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePaymentReceiveResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePaymentReceiveAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeletePaymentReceive")]
    public async Task<ActionResult<ApiSuccessResult<DeletePaymentReceiveResult>>> DeletePaymentReceiveAsync(DeletePaymentReceiveRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeletePaymentReceiveResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeletePaymentReceiveAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPaymentReceiveList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentReceiveListResult>>> GetPaymentReceiveListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetPaymentReceiveListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentReceiveListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentReceiveListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPaymentReceiveStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentReceiveStatusListResult>>> GetPaymentReceiveStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetPaymentReceiveStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentReceiveStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentReceiveStatusListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPaymentReceiveSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentReceiveSingleResult>>> GetPaymentReceiveSingleAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetPaymentReceiveSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentReceiveSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentReceiveSingleAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPaymentReceiveByInvoiceIdList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentReceiveByInvoiceIdListResult>>> GetPaymentReceiveByInvoiceIdListAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetPaymentReceiveByInvoiceIdListRequest { InvoiceId = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentReceiveByInvoiceIdListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentReceiveByInvoiceIdListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPaymentReceiveReport")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentReceiveReportResult>>> GetPaymentReceiveReportAsync(
    CancellationToken cancellationToken
    )
    {
        var request = new GetPaymentReceiveReportRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentReceiveReportResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentReceiveReportAsync)}",
            Content = response
        });
    }


}


