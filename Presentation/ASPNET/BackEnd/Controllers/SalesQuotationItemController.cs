using Application.Features.SalesQuotationItemManager.Commands;
using Application.Features.SalesQuotationItemManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class SalesQuotationItemController : BaseApiController
{
    public SalesQuotationItemController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateSalesQuotationItem")]
    public async Task<ActionResult<ApiSuccessResult<CreateSalesQuotationItemResult>>> CreateSalesQuotationItemAsync(CreateSalesQuotationItemRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateSalesQuotationItemResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateSalesQuotationItemAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateSalesQuotationItem")]
    public async Task<ActionResult<ApiSuccessResult<UpdateSalesQuotationItemResult>>> UpdateSalesQuotationItemAsync(UpdateSalesQuotationItemRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateSalesQuotationItemResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateSalesQuotationItemAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteSalesQuotationItem")]
    public async Task<ActionResult<ApiSuccessResult<DeleteSalesQuotationItemResult>>> DeleteSalesQuotationItemAsync(DeleteSalesQuotationItemRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteSalesQuotationItemResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteSalesQuotationItemAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetSalesQuotationItemList")]
    public async Task<ActionResult<ApiSuccessResult<GetSalesQuotationItemListResult>>> GetSalesQuotationItemListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetSalesQuotationItemListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetSalesQuotationItemListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetSalesQuotationItemListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetSalesQuotationItemBySalesQuotationIdList")]
    public async Task<ActionResult<ApiSuccessResult<GetSalesQuotationItemBySalesQuotationIdListResult>>> GetSalesQuotationItemBySalesQuotationIdListAsync(
    CancellationToken cancellationToken,
    [FromQuery] string salesQuotationId
    )
    {
        var request = new GetSalesQuotationItemBySalesQuotationIdListRequest { SalesQuotationId = salesQuotationId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetSalesQuotationItemBySalesQuotationIdListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetSalesQuotationItemBySalesQuotationIdListAsync)}",
            Content = response
        });
    }


}


