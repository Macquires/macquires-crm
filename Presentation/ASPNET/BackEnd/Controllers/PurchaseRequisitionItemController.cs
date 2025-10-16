using Application.Features.PurchaseRequisitionItemManager.Commands;
using Application.Features.PurchaseRequisitionItemManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PurchaseRequisitionItemController : BaseApiController
{
    public PurchaseRequisitionItemController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreatePurchaseRequisitionItem")]
    public async Task<ActionResult<ApiSuccessResult<CreatePurchaseRequisitionItemResult>>> CreatePurchaseRequisitionItemAsync(CreatePurchaseRequisitionItemRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreatePurchaseRequisitionItemResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreatePurchaseRequisitionItemAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdatePurchaseRequisitionItem")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePurchaseRequisitionItemResult>>> UpdatePurchaseRequisitionItemAsync(UpdatePurchaseRequisitionItemRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePurchaseRequisitionItemResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePurchaseRequisitionItemAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeletePurchaseRequisitionItem")]
    public async Task<ActionResult<ApiSuccessResult<DeletePurchaseRequisitionItemResult>>> DeletePurchaseRequisitionItemAsync(DeletePurchaseRequisitionItemRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeletePurchaseRequisitionItemResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeletePurchaseRequisitionItemAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPurchaseRequisitionItemList")]
    public async Task<ActionResult<ApiSuccessResult<GetPurchaseRequisitionItemListResult>>> GetPurchaseRequisitionItemListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetPurchaseRequisitionItemListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPurchaseRequisitionItemListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPurchaseRequisitionItemListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPurchaseRequisitionItemByPurchaseRequisitionIdList")]
    public async Task<ActionResult<ApiSuccessResult<GetPurchaseRequisitionItemByPurchaseRequisitionIdListResult>>> GetPurchaseRequisitionItemByPurchaseRequisitionIdListAsync(
    CancellationToken cancellationToken,
    [FromQuery] string purchaseRequisitionId
    )
    {
        var request = new GetPurchaseRequisitionItemByPurchaseRequisitionIdListRequest { PurchaseRequisitionId = purchaseRequisitionId };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPurchaseRequisitionItemByPurchaseRequisitionIdListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPurchaseRequisitionItemByPurchaseRequisitionIdListAsync)}",
            Content = response
        });
    }


}


