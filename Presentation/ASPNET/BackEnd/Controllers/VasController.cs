using Application.Features.VasManager.Commands;
using Application.Features.VasManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
[Authorize]
public class VasController : BaseApiController
{
    public VasController(ISender sender) : base(sender)
    {
    }

    [HttpGet("GetValueAddedServiceList")]
    public async Task<ActionResult<ApiSuccessResult<GetValueAddedServiceListResult>>> GetValueAddedServiceListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] bool activeOnly = false)
    {
        var response = await _sender.Send(
            new GetValueAddedServiceListRequest { IsDeleted = isDeleted, ActiveOnly = activeOnly },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetValueAddedServiceListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetValueAddedServiceListAsync),
            Content = response,
        });
    }

    [HttpGet("GetSubscriberVasPanel")]
    public async Task<ActionResult<ApiSuccessResult<GetSubscriberVasPanelResult>>> GetSubscriberVasPanelAsync(
        [FromQuery] string? msisdn,
        [FromQuery] string? telecomSubscriptionId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetSubscriberVasPanelRequest { Msisdn = msisdn, TelecomSubscriptionId = telecomSubscriptionId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetSubscriberVasPanelResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetSubscriberVasPanelAsync),
            Content = response,
        });
    }

    [HttpPost("CreateValueAddedService")]
    public async Task<ActionResult<ApiSuccessResult<CreateValueAddedServiceResult>>> CreateValueAddedServiceAsync(
        CreateValueAddedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateValueAddedServiceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreateValueAddedServiceAsync),
            Content = response,
        });
    }

    [HttpPost("UpdateValueAddedService")]
    public async Task<ActionResult<ApiSuccessResult<UpdateValueAddedServiceResult>>> UpdateValueAddedServiceAsync(
        UpdateValueAddedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateValueAddedServiceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UpdateValueAddedServiceAsync),
            Content = response,
        });
    }

    [HttpPost("DeleteValueAddedService")]
    public async Task<ActionResult<ApiSuccessResult<DeleteValueAddedServiceResult>>> DeleteValueAddedServiceAsync(
        DeleteValueAddedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<DeleteValueAddedServiceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(DeleteValueAddedServiceAsync),
            Content = response,
        });
    }

    [HttpPost("ToggleSubscriberVasService")]
    public async Task<ActionResult<ApiSuccessResult<ToggleSubscriberVasServiceResult>>> ToggleSubscriberVasServiceAsync(
        ToggleSubscriberVasServiceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ToggleSubscriberVasServiceResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ToggleSubscriberVasServiceAsync),
            Content = response,
        });
    }
}
