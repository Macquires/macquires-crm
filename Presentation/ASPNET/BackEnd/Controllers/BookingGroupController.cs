using Application.Features.BookingGroupManager.Commands;
using Application.Features.BookingGroupManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class BookingGroupController : BaseApiController
{
    public BookingGroupController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateBookingGroup")]
    public async Task<ActionResult<ApiSuccessResult<CreateBookingGroupResult>>> CreateBookingGroupAsync(CreateBookingGroupRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateBookingGroupResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateBookingGroupAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateBookingGroup")]
    public async Task<ActionResult<ApiSuccessResult<UpdateBookingGroupResult>>> UpdateBookingGroupAsync(UpdateBookingGroupRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateBookingGroupResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBookingGroupAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteBookingGroup")]
    public async Task<ActionResult<ApiSuccessResult<DeleteBookingGroupResult>>> DeleteBookingGroupAsync(DeleteBookingGroupRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteBookingGroupResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteBookingGroupAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetBookingGroupList")]
    public async Task<ActionResult<ApiSuccessResult<GetBookingGroupListResult>>> GetBookingGroupListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetBookingGroupListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBookingGroupListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBookingGroupListAsync)}",
            Content = response
        });
    }


}


