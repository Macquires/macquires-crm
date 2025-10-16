using Application.Features.PurchaseRequisitionManager.Commands;
using Application.Features.PurchaseRequisitionManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class PurchaseRequisitionController : BaseApiController
{
    public PurchaseRequisitionController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreatePurchaseRequisition")]
    public async Task<ActionResult<ApiSuccessResult<CreatePurchaseRequisitionResult>>> CreatePurchaseRequisitionAsync(CreatePurchaseRequisitionRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreatePurchaseRequisitionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreatePurchaseRequisitionAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdatePurchaseRequisition")]
    public async Task<ActionResult<ApiSuccessResult<UpdatePurchaseRequisitionResult>>> UpdatePurchaseRequisitionAsync(UpdatePurchaseRequisitionRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdatePurchaseRequisitionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdatePurchaseRequisitionAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeletePurchaseRequisition")]
    public async Task<ActionResult<ApiSuccessResult<DeletePurchaseRequisitionResult>>> DeletePurchaseRequisitionAsync(DeletePurchaseRequisitionRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeletePurchaseRequisitionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeletePurchaseRequisitionAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetPurchaseRequisitionList")]
    public async Task<ActionResult<ApiSuccessResult<GetPurchaseRequisitionListResult>>> GetPurchaseRequisitionListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetPurchaseRequisitionListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPurchaseRequisitionListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPurchaseRequisitionListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPurchaseRequisitionStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetPurchaseRequisitionStatusListResult>>> GetPurchaseRequisitionStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetPurchaseRequisitionStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPurchaseRequisitionStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPurchaseRequisitionStatusListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetPurchaseRequisitionSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetPurchaseRequisitionSingleResult>>> GetPurchaseRequisitionSingleAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetPurchaseRequisitionSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetPurchaseRequisitionSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetPurchaseRequisitionSingleAsync)}",
            Content = response
        });
    }



}


