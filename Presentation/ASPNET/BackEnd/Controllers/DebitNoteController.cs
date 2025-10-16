using Application.Features.DebitNoteManager.Commands;
using Application.Features.DebitNoteManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class DebitNoteController : BaseApiController
{
    public DebitNoteController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateDebitNote")]
    public async Task<ActionResult<ApiSuccessResult<CreateDebitNoteResult>>> CreateDebitNoteAsync(CreateDebitNoteRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateDebitNoteResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateDebitNoteAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateDebitNote")]
    public async Task<ActionResult<ApiSuccessResult<UpdateDebitNoteResult>>> UpdateDebitNoteAsync(UpdateDebitNoteRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateDebitNoteResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateDebitNoteAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteDebitNote")]
    public async Task<ActionResult<ApiSuccessResult<DeleteDebitNoteResult>>> DeleteDebitNoteAsync(DeleteDebitNoteRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteDebitNoteResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteDebitNoteAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetDebitNoteList")]
    public async Task<ActionResult<ApiSuccessResult<GetDebitNoteListResult>>> GetDebitNoteListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetDebitNoteListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetDebitNoteListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetDebitNoteListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetDebitNoteStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetDebitNoteStatusListResult>>> GetDebitNoteStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetDebitNoteStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetDebitNoteStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetDebitNoteStatusListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetDebitNoteSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetDebitNoteSingleResult>>> GetDebitNoteSingleAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetDebitNoteSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetDebitNoteSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetDebitNoteSingleAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetDebitNoteByPurchaseReturnIdList")]
    public async Task<ActionResult<ApiSuccessResult<GetDebitNoteByPurchaseReturnIdListResult>>> GetDebitNoteByPurchaseReturnIdListAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetDebitNoteByPurchaseReturnIdListRequest { PurchaseReturnId = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetDebitNoteByPurchaseReturnIdListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetDebitNoteByPurchaseReturnIdListAsync)}",
            Content = response
        });
    }


}


