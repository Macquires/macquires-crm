using Application.Features.ProgramManagerManager.Commands;
using Application.Features.ProgramManagerManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class ProgramManagerController : BaseApiController
{
    public ProgramManagerController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateProgramManager")]
    public async Task<ActionResult<ApiSuccessResult<CreateProgramManagerResult>>> CreateProgramManagerAsync(CreateProgramManagerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateProgramManagerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateProgramManagerAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateProgramManager")]
    public async Task<ActionResult<ApiSuccessResult<UpdateProgramManagerResult>>> UpdateProgramManagerAsync(UpdateProgramManagerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateProgramManagerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateProgramManagerAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteProgramManager")]
    public async Task<ActionResult<ApiSuccessResult<DeleteProgramManagerResult>>> DeleteProgramManagerAsync(DeleteProgramManagerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteProgramManagerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteProgramManagerAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetProgramManagerList")]
    public async Task<ActionResult<ApiSuccessResult<GetProgramManagerListResult>>> GetProgramManagerListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetProgramManagerListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProgramManagerListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProgramManagerListAsync)}",
            Content = response
        });
    }



    [Authorize]
    [HttpGet("GetProgramManagerStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetProgramManagerStatusListResult>>> GetProgramManagerStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetProgramManagerStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProgramManagerStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProgramManagerStatusListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetProgramManagerPriorityList")]
    public async Task<ActionResult<ApiSuccessResult<GetProgramManagerPriorityListResult>>> GetProgramManagerPriorityListAsync(
    CancellationToken cancellationToken
    )
    {
        var request = new GetProgramManagerPriorityListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetProgramManagerPriorityListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetProgramManagerPriorityListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("KanbanGet")]
    public async Task<ActionResult<ApiSuccessResult<GetProgramManagerKanbanListResult>>> KanbanGetAsync(GetProgramManagerKanbanListRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(response?.Data);
    }

    [Authorize]
    [HttpPost("KanbanUpdate")]
    public async Task<ActionResult<ApiSuccessResult<KanbanUpdateResult>>> KanbanUpdateAsync(KanbanUpdateRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(response?.Data);
    }

    [Authorize]
    [HttpPost("KanbanDelete")]
    public async Task<ActionResult<ApiSuccessResult<KanbanDeleteResult>>> KanbanDeleteAsync(KanbanDeleteRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(response?.Data);
    }

}


