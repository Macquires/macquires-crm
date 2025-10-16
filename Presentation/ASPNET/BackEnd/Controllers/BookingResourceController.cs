using Application.Features.BookingResourceManager.Commands;
using Application.Features.BookingResourceManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class BookingResourceController : BaseApiController
{
    public BookingResourceController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateBookingResource")]
    public async Task<ActionResult<ApiSuccessResult<CreateBookingResourceResult>>> CreateBookingResourceAsync(CreateBookingResourceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateBookingResourceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateBookingResourceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateBookingResource")]
    public async Task<ActionResult<ApiSuccessResult<UpdateBookingResourceResult>>> UpdateBookingResourceAsync(UpdateBookingResourceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateBookingResourceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBookingResourceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteBookingResource")]
    public async Task<ActionResult<ApiSuccessResult<DeleteBookingResourceResult>>> DeleteBookingResourceAsync(DeleteBookingResourceRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteBookingResourceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteBookingResourceAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetBookingResourceList")]
    public async Task<ActionResult<ApiSuccessResult<GetBookingResourceListResult>>> GetBookingResourceListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetBookingResourceListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBookingResourceListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBookingResourceListAsync)}",
            Content = response
        });
    }


}


