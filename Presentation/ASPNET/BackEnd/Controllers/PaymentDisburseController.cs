using Application.Features.PaymentDisburseManager.Commands;
using Application.Features.PaymentDisburseManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PaymentDisburseController : BaseApiController
{
    public PaymentDisburseController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreatePaymentDisburse")]
    public async Task<ActionResult<ApiSuccessResult<CreatePaymentDisburseResult>>> CreatePaymentDisburseAsync(CreatePaymentDisburseRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreatePaymentDisburseResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreatePaymentDisburseAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdatePaymentDisburse")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePaymentDisburseResult>>> UpdatePaymentDisburseAsync(UpdatePaymentDisburseRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePaymentDisburseResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePaymentDisburseAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeletePaymentDisburse")]
    public async Task<ActionResult<ApiSuccessResult<DeletePaymentDisburseResult>>> DeletePaymentDisburseAsync(DeletePaymentDisburseRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeletePaymentDisburseResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeletePaymentDisburseAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPaymentDisburseList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentDisburseListResult>>> GetPaymentDisburseListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetPaymentDisburseListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentDisburseListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentDisburseListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPaymentDisburseStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentDisburseStatusListResult>>> GetPaymentDisburseStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetPaymentDisburseStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentDisburseStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentDisburseStatusListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPaymentDisburseSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentDisburseSingleResult>>> GetPaymentDisburseSingleAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetPaymentDisburseSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentDisburseSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentDisburseSingleAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPaymentDisburseByBillIdList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentDisburseByBillIdListResult>>> GetPaymentDisburseByBillIdListAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetPaymentDisburseByBillIdListRequest { BillId = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentDisburseByBillIdListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentDisburseByBillIdListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPaymentDisburseReport")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentDisburseReportResult>>> GetPaymentDisburseReportAsync(
    CancellationToken cancellationToken
    )
    {
        var request = new GetPaymentDisburseReportRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPaymentDisburseReportResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPaymentDisburseReportAsync)}",
            Content = response
        });
    }


}


