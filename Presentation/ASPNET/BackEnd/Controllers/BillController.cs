using Application.Features.BillManager.Commands;
using Application.Features.BillManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class BillController : BaseApiController
{
    public BillController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateBill")]
    public async Task<ActionResult<ApiSuccessResult<CreateBillResult>>> CreateBillAsync(CreateBillRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateBillResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateBillAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateBill")]
    public async Task<ActionResult<ApiSuccessResult<UpdateBillResult>>> UpdateBillAsync(UpdateBillRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateBillResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBillAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteBill")]
    public async Task<ActionResult<ApiSuccessResult<DeleteBillResult>>> DeleteBillAsync(DeleteBillRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteBillResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteBillAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetBillList")]
    public async Task<ActionResult<ApiSuccessResult<GetBillListResult>>> GetBillListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetBillListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBillListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBillListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetBillStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetBillStatusListResult>>> GetBillStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetBillStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBillStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBillStatusListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetBillSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetBillSingleResult>>> GetBillSingleAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetBillSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBillSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBillSingleAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetBillByPurchaseOrderIdList")]
    public async Task<ActionResult<ApiSuccessResult<GetBillByPurchaseOrderIdListResult>>> GetBillByPurchaseOrderIdListAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetBillByPurchaseOrderIdListRequest { PurchaseOrderId = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBillByPurchaseOrderIdListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBillByPurchaseOrderIdListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetBillReport")]
    public async Task<ActionResult<ApiSuccessResult<GetBillReportResult>>> GetBillReportAsync(
    CancellationToken cancellationToken
    )
    {
        var request = new GetBillReportRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBillReportResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBillReportAsync)}",
            Content = response
        });
    }


}


