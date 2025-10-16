using Application.Features.CreditNoteManager.Commands;
using Application.Features.CreditNoteManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class CreditNoteController : BaseApiController
{
    public CreditNoteController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateCreditNote")]
    public async Task<ActionResult<ApiSuccessResult<CreateCreditNoteResult>>> CreateCreditNoteAsync(CreateCreditNoteRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateCreditNoteResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateCreditNoteAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateCreditNote")]
    public async Task<ActionResult<ApiSuccessResult<UpdateCreditNoteResult>>> UpdateCreditNoteAsync(UpdateCreditNoteRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateCreditNoteResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateCreditNoteAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteCreditNote")]
    public async Task<ActionResult<ApiSuccessResult<DeleteCreditNoteResult>>> DeleteCreditNoteAsync(DeleteCreditNoteRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteCreditNoteResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteCreditNoteAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetCreditNoteList")]
    public async Task<ActionResult<ApiSuccessResult<GetCreditNoteListResult>>> GetCreditNoteListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetCreditNoteListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCreditNoteListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCreditNoteListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetCreditNoteStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetCreditNoteStatusListResult>>> GetCreditNoteStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetCreditNoteStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCreditNoteStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCreditNoteStatusListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetCreditNoteSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetCreditNoteSingleResult>>> GetCreditNoteSingleAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetCreditNoteSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCreditNoteSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCreditNoteSingleAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetCreditNoteBySalesReturnIdList")]
    public async Task<ActionResult<ApiSuccessResult<GetCreditNoteBySalesReturnIdListResult>>> GetCreditNoteBySalesReturnIdListAsync(
    CancellationToken cancellationToken,
    [FromQuery] string id
    )
    {
        var request = new GetCreditNoteBySalesReturnIdListRequest { SalesReturnId = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetCreditNoteBySalesReturnIdListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetCreditNoteBySalesReturnIdListAsync)}",
            Content = response
        });
    }


}


