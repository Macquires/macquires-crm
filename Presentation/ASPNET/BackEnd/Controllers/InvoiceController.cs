using Application.Features.InvoiceManager.Commands;
using Application.Features.InvoiceManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class InvoiceController : BaseApiController
{
    public InvoiceController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateInvoice")]
    public async Task<ActionResult<ApiSuccessResult<CreateInvoiceResult>>> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateInvoiceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateInvoiceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateInvoice")]
    public async Task<ActionResult<ApiSuccessResult<UpdateInvoiceResult>>> UpdateInvoiceAsync(UpdateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateInvoiceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateInvoiceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteInvoice")]
    public async Task<ActionResult<ApiSuccessResult<DeleteInvoiceResult>>> DeleteInvoiceAsync(DeleteInvoiceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteInvoiceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteInvoiceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetInvoiceList")]
    public async Task<ActionResult<ApiSuccessResult<GetInvoiceListResult>>> GetInvoiceListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetInvoiceListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetInvoiceListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetInvoiceListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetInvoiceStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetInvoiceStatusListResult>>> GetInvoiceStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetInvoiceStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetInvoiceStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetInvoiceStatusListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetInvoiceSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetInvoiceSingleResult>>> GetInvoiceSingleAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetInvoiceSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetInvoiceSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetInvoiceSingleAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetInvoiceBySalesOrderIdList")]
    public async Task<ActionResult<ApiSuccessResult<GetInvoiceBySalesOrderIdListResult>>> GetInvoiceBySalesOrderIdListAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetInvoiceBySalesOrderIdListRequest { SalesOrderId = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetInvoiceBySalesOrderIdListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetInvoiceBySalesOrderIdListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetInvoiceReport")]
    public async Task<ActionResult<ApiSuccessResult<GetInvoiceReportResult>>> GetInvoiceReportAsync(
    CancellationToken cancellationToken
    )
    {
        var request = new GetInvoiceReportRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetInvoiceReportResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetInvoiceReportAsync)}",
            Content = response
        });
    }


}


