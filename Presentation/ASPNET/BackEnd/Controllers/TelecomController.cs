using Application.Common.Integrations;
using Application.Features.TelecomManager.Commands;
using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using Infrastructure.SecurityManager.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class TelecomController : BaseApiController
{
    private readonly IChargingSystemIntegration _charging;
    private readonly ISmsGatewayIntegration _sms;

    public TelecomController(ISender sender, IChargingSystemIntegration charging, ISmsGatewayIntegration sms) : base(sender)
    {
        _charging = charging;
        _sms = sms;
    }

    [Authorize(Roles = TelecomRoles.RolesCreateOperation)]
    [HttpPost("CreateTelecomOperation")]
    public async Task<ActionResult<ApiSuccessResult<CreateTelecomOperationRequestResult>>> CreateTelecomOperationAsync(
        CreateTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateTelecomOperationRequestResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreateTelecomOperationAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesUploadDocument)]
    [HttpPost("UploadTelecomOperationDocument")]
    public async Task<ActionResult<ApiSuccessResult<UploadTelecomOperationDocumentResult>>> UploadTelecomOperationDocumentAsync(
        UploadTelecomOperationDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UploadTelecomOperationDocumentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UploadTelecomOperationDocumentAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesConfirmOperation)]
    [HttpPost("ConfirmTelecomOperation")]
    public async Task<ActionResult<ApiSuccessResult<ConfirmTelecomOperationRequestResult>>> ConfirmTelecomOperationAsync(
        ConfirmTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ConfirmTelecomOperationRequestResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ConfirmTelecomOperationAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesImportSim)]
    [HttpPost("ImportSimInventoryBatch")]
    public async Task<ActionResult<ApiSuccessResult<ImportSimInventoryBatchResult>>> ImportSimInventoryBatchAsync(
        ImportSimInventoryBatchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ImportSimInventoryBatchResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ImportSimInventoryBatchAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetTelecomOperationList")]
    public async Task<ActionResult<ApiSuccessResult<GetTelecomOperationListResult>>> GetTelecomOperationListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false)
    {
        var response = await _sender.Send(new GetTelecomOperationListRequest { IsDeleted = isDeleted }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTelecomOperationListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTelecomOperationListAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetBillingIntegrationLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetBillingIntegrationLogListResult>>> GetBillingIntegrationLogListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? telecomOperationRequestId = null,
        [FromQuery] bool isDeleted = false)
    {
        var response = await _sender.Send(new GetBillingIntegrationLogListRequest
        {
            TelecomOperationRequestId = telecomOperationRequestId,
            IsDeleted = isDeleted
        }, cancellationToken);

        return Ok(new ApiSuccessResult<GetBillingIntegrationLogListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetBillingIntegrationLogListAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetTelecomDashboardKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetTelecomDashboardKpisResult>>> GetTelecomDashboardKpisAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetTelecomDashboardKpisRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetTelecomDashboardKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTelecomDashboardKpisAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetTelecomUniversalSearch")]
    public async Task<ActionResult<ApiSuccessResult<GetTelecomUniversalSearchResult>>> GetTelecomUniversalSearchAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? term = null)
    {
        var response = await _sender.Send(new GetTelecomUniversalSearchRequest { Term = term }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTelecomUniversalSearchResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTelecomUniversalSearchAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesDemoIntegrations)]
    [HttpPost("TouchChargingDemo")]
    public async Task<ActionResult<ApiSuccessResult<BillingProvisionResult>>> TouchChargingDemoAsync(
        CancellationToken cancellationToken,
        [FromQuery] string correlationId = "demo")
    {
        var r = await _charging.TouchAsync(correlationId, cancellationToken);
        return Ok(new ApiSuccessResult<BillingProvisionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(TouchChargingDemoAsync),
            Content = r
        });
    }

    [Authorize(Roles = TelecomRoles.RolesDemoIntegrations)]
    [HttpPost("SendSmsDemo")]
    public async Task<ActionResult<ApiSuccessResult<BillingProvisionResult>>> SendSmsDemoAsync(
        CancellationToken cancellationToken,
        [FromQuery] string phoneNumber = "0939000001",
        [FromQuery] string body = "Syriatel demo SMS")
    {
        var r = await _sms.SendAsync(phoneNumber, body, cancellationToken);
        return Ok(new ApiSuccessResult<BillingProvisionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(SendSmsDemoAsync),
            Content = r
        });
    }
}
