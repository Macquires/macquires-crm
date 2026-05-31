using Application.Features.TelecomSubscriptionTypeManager.Commands;
using Application.Features.TelecomSubscriptionTypeManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using Infrastructure.SecurityManager.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class TelecomSubscriptionTypeController : BaseApiController
{
    public TelecomSubscriptionTypeController(ISender sender) : base(sender)
    {
    }

    [Authorize]
    [HttpGet("GetTelecomSubscriptionTypeList")]
    public async Task<ActionResult<ApiSuccessResult<GetTelecomSubscriptionTypeListResult>>> GetTelecomSubscriptionTypeListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] bool activeOnly = false)
    {
        var response = await _sender.Send(
            new GetTelecomSubscriptionTypeListRequest { IsDeleted = isDeleted, ActiveOnly = activeOnly },
            cancellationToken);

        return Ok(new ApiSuccessResult<GetTelecomSubscriptionTypeListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTelecomSubscriptionTypeListAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesManageTelecomLineTypes)]
    [HttpPost("CreateTelecomSubscriptionType")]
    public async Task<ActionResult<ApiSuccessResult<CreateTelecomSubscriptionTypeResult>>> CreateTelecomSubscriptionTypeAsync(
        CreateTelecomSubscriptionTypeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateTelecomSubscriptionTypeResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreateTelecomSubscriptionTypeAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesManageTelecomLineTypes)]
    [HttpPost("UpdateTelecomSubscriptionType")]
    public async Task<ActionResult<ApiSuccessResult<UpdateTelecomSubscriptionTypeResult>>> UpdateTelecomSubscriptionTypeAsync(
        UpdateTelecomSubscriptionTypeRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateTelecomSubscriptionTypeResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UpdateTelecomSubscriptionTypeAsync),
            Content = response
        });
    }
}
