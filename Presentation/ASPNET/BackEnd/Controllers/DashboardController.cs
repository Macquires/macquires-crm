using Application.Features.DashboardManager.Commands;
using Application.Features.DashboardManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class DashboardController : BaseApiController
{
    public DashboardController(ISender sender) : base(sender)
    {
    }

    private List<string> GetUserRoles() =>
        User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

    [RequireTelecomRead]
    [HttpGet("GetMyWidgets")]
    public async Task<ActionResult<ApiSuccessResult<GetMyDashboardWidgetsResult>>> GetMyWidgetsAsync(
        [FromQuery] string? previewPersona,
        CancellationToken cancellationToken)
    {
        // Widget definitions are cached in memory; avoid failing when the browser aborts a duplicate request.
        var response = await _sender.Send(
            new GetMyDashboardWidgetsRequest { Roles = GetUserRoles(), PreviewPersona = previewPersona },
            CancellationToken.None);

        return Ok(new ApiSuccessResult<GetMyDashboardWidgetsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetMyWidgetsAsync),
            Content = response,
        });
    }

    [RequireTelecomRead]
    [HttpGet("GetWidgetData")]
    public async Task<ActionResult<ApiSuccessResult<GetDashboardWidgetDataResult>>> GetWidgetDataAsync(
        [FromQuery] string providerKey,
        [FromQuery] string? previewPersona,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetDashboardWidgetDataRequest
            {
                Roles = GetUserRoles(),
                ProviderKey = providerKey,
                PreviewPersona = previewPersona,
            },
            CancellationToken.None);

        return Ok(new ApiSuccessResult<GetDashboardWidgetDataResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetWidgetDataAsync),
            Content = response,
        });
    }

    [RequireTelecomRead]
    [HttpPost("GetWidgetDataBatch")]
    public async Task<ActionResult<ApiSuccessResult<GetDashboardWidgetDataBatchResult>>> GetWidgetDataBatchAsync(
        GetDashboardWidgetDataBatchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new GetDashboardWidgetDataBatchRequest
            {
                Roles = GetUserRoles(),
                ProviderKeys = request.ProviderKeys,
                PreviewPersona = request.PreviewPersona,
            },
            CancellationToken.None);

        return Ok(new ApiSuccessResult<GetDashboardWidgetDataBatchResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetWidgetDataBatchAsync),
            Content = response,
        });
    }

    [RequireDashboardAdmin]
    [HttpGet("GetRegisteredProviders")]
    public async Task<ActionResult<ApiSuccessResult<GetRegisteredDashboardProvidersResult>>> GetRegisteredProvidersAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetRegisteredDashboardProvidersRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetRegisteredDashboardProvidersResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetRegisteredProvidersAsync),
            Content = response,
        });
    }

    [RequireDashboardAdmin]
    [HttpGet("GetDashboardWidgetList")]
    public async Task<ActionResult<ApiSuccessResult<GetDashboardWidgetListResult>>> GetDashboardWidgetListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] bool activeOnly = false)
    {
        var response = await _sender.Send(
            new GetDashboardWidgetListRequest { IsDeleted = isDeleted, ActiveOnly = activeOnly },
            cancellationToken);

        return Ok(new ApiSuccessResult<GetDashboardWidgetListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetDashboardWidgetListAsync),
            Content = response,
        });
    }

    [RequireDashboardAdmin]
    [HttpPost("CreateDashboardWidget")]
    public async Task<ActionResult<ApiSuccessResult<CreateDashboardWidgetResult>>> CreateDashboardWidgetAsync(
        CreateDashboardWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateDashboardWidgetResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreateDashboardWidgetAsync),
            Content = response,
        });
    }

    [RequireDashboardAdmin]
    [HttpPost("UpdateDashboardWidget")]
    public async Task<ActionResult<ApiSuccessResult<UpdateDashboardWidgetResult>>> UpdateDashboardWidgetAsync(
        UpdateDashboardWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateDashboardWidgetResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UpdateDashboardWidgetAsync),
            Content = response,
        });
    }

    [RequireDashboardAdmin]
    [HttpPost("DeleteDashboardWidget")]
    public async Task<ActionResult<ApiSuccessResult<DeleteDashboardWidgetResult>>> DeleteDashboardWidgetAsync(
        DeleteDashboardWidgetRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<DeleteDashboardWidgetResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(DeleteDashboardWidgetAsync),
            Content = response,
        });
    }

    [RequireDashboardAdmin]
    [HttpPost("ReorderDashboardWidgets")]
    public async Task<ActionResult<ApiSuccessResult<ReorderDashboardWidgetsResult>>> ReorderDashboardWidgetsAsync(
        ReorderDashboardWidgetsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ReorderDashboardWidgetsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ReorderDashboardWidgetsAsync),
            Content = response,
        });
    }
}
