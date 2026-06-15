using Application.Features.TelecomManager.Commands;
using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Integrations;

using ASPNET.BackEnd.Common.Base;
using MediatR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/Telecom")]
public class TelecomDemosController : BaseApiController
{
    private readonly IChargingSystemIntegration _charging;
    private readonly ISmsGatewayIntegration _sms;

    public TelecomDemosController(
        ISender sender,
        IChargingSystemIntegration charging,
        ISmsGatewayIntegration sms) : base(sender)
    {
        _charging = charging;
        _sms = sms;
    }

    [RequireTelecomAdminSettings]
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

    [RequireTelecomAdminSettings]
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
