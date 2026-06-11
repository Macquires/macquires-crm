using Application.Features.GeoCityManager.Commands;
using Application.Features.GeoCityManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class GeoCityController : BaseApiController
{
    public GeoCityController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpPost("CreateGeoCity")]
    public async Task<ActionResult<ApiSuccessResult<CreateGeoCityResult>>> CreateGeoCityAsync(
        CreateGeoCityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<CreateGeoCityResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(CreateGeoCityAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("UpdateGeoCity")]
    public async Task<ActionResult<ApiSuccessResult<UpdateGeoCityResult>>> UpdateGeoCityAsync(
        UpdateGeoCityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<UpdateGeoCityResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(UpdateGeoCityAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpPost("DeleteGeoCity")]
    public async Task<ActionResult<ApiSuccessResult<DeleteGeoCityResult>>> DeleteGeoCityAsync(
        DeleteGeoCityRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<DeleteGeoCityResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(DeleteGeoCityAsync)}",
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetGeoCityList")]
    public async Task<ActionResult<ApiSuccessResult<GetGeoCityListResult>>> GetGeoCityListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] bool activeOnly = false)
    {
        var request = new GetGeoCityListRequest
        {
            IsDeleted = isDeleted,
            ActiveOnly = activeOnly
        };
        var response = await _sender.Send(request, cancellationToken);

        return Ok(new ApiSuccessResult<GetGeoCityListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = $"Success executing {nameof(GetGeoCityListAsync)}",
            Content = response
        });
    }
}
