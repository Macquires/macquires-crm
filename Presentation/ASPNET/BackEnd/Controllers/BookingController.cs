using Application.Features.BookingManager.Commands;
using Application.Features.BookingManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class BookingController : BaseApiController
{
    public BookingController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateBooking")]
    public async Task<ActionResult<ApiSuccessResult<CreateBookingResult>>> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateBookingResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateBookingAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateBooking")]
    public async Task<ActionResult<ApiSuccessResult<UpdateBookingResult>>> UpdateBookingAsync(UpdateBookingRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateBookingResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateBookingAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteBooking")]
    public async Task<ActionResult<ApiSuccessResult<DeleteBookingResult>>> DeleteBookingAsync(DeleteBookingRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteBookingResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteBookingAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetBookingSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetBookingSingleResult>>> GetBookingSingleAsync(
        CancellationToken cancellationToken,
        [FromQuery] string id
        )
    {
        var request = new GetBookingSingleRequest { Id = id };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBookingSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBookingSingleAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetBookingList")]
    public async Task<ActionResult<ApiSuccessResult<GetBookingListResult>>> GetBookingListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false
        )
    {
        var request = new GetBookingListRequest { IsDeleted = isDeleted };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBookingListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBookingListAsync)}",
            Content = response
        });
    }


    [Authorize]
    [HttpGet("GetBookingStatusList")]
    public async Task<ActionResult<ApiSuccessResult<GetBookingStatusListResult>>> GetBookingStatusListAsync(
        CancellationToken cancellationToken
        )
    {
        var request = new GetBookingStatusListRequest { };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetBookingStatusListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetBookingStatusListAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("SchedulerGet")]
    public async Task<ActionResult<GetSchedulerResult>> GetSchedulerAsync(GetSchedulerRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(response.Data);
    }

    [Authorize]
    [HttpPost("SchedulerCRUD")]
    public async Task<ActionResult<SchedulerCRUDResult>> HandleSchedulerCRUDAsync(SchedulerCRUDRequest request, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(response);
    }

}


