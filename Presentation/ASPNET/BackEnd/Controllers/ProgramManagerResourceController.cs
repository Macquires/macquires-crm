using Application.Features.ProgramManagerResourceManager.Commands;
using Application.Features.ProgramManagerResourceManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class ProgramManagerResourceController : BaseApiController
{
    public ProgramManagerResourceController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateProgramManagerResource")]
    public async Task<ActionResult<ApiSuccessResult<CreateProgramManagerResourceResult>>> CreateProgramManagerResourceAsync(CreateProgramManagerResourceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateProgramManagerResourceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateProgramManagerResourceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateProgramManagerResource")]
    public async Task<ActionResult<ApiSuccessResult<UpdateProgramManagerResourceResult>>> UpdateProgramManagerResourceAsync(UpdateProgramManagerResourceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateProgramManagerResourceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateProgramManagerResourceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteProgramManagerResource")]
    public async Task<ActionResult<ApiSuccessResult<DeleteProgramManagerResourceResult>>> DeleteProgramManagerResourceAsync(DeleteProgramManagerResourceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteProgramManagerResourceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteProgramManagerResourceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetProgramManagerResourceList")]
    public async Task<ActionResult<ApiSuccessResult<GetProgramManagerResourceListResult>>> GetProgramManagerResourceListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetProgramManagerResourceListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProgramManagerResourceListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProgramManagerResourceListAsync)}",
            Content = response
        });
    }


}


