using System.Security.Claims;
using Application.Common.Integrations;
using Application.Features.TelecomBackOfficeManager.Commands;
using Application.Features.TelecomBackOfficeManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using Infrastructure.SecurityManager.Roles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class TelecomBackOfficeController : BaseApiController
{
    public TelecomBackOfficeController(ISender sender) : base(sender)
    {
    }

    private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [Authorize(Roles = TelecomRoles.RolesManageTechnicalTickets)]
    [HttpGet("GetTechnicalTickets")]
    public async Task<ActionResult<ApiSuccessResult<GetTechnicalTicketsResult>>> GetTechnicalTicketsAsync(
        [FromQuery] GetTechnicalTicketsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<GetTechnicalTicketsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTechnicalTicketsAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesManageTechnicalTickets)]
    [HttpGet("GetTechnicalTicketSingle")]
    public async Task<ActionResult<ApiSuccessResult<GetTechnicalTicketSingleResult>>> GetTechnicalTicketSingleAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetTechnicalTicketSingleRequest { Id = id }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTechnicalTicketSingleResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTechnicalTicketSingleAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesManageTechnicalTickets)]
    [HttpPost("SimulateVoiceAiIncomingCall")]
    public async Task<ActionResult<ApiSuccessResult<SimulateVoiceAiIncomingCallResult>>> SimulateVoiceAiIncomingCallAsync(
        [FromBody] SimulateVoiceAiIncomingCallRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new SimulateVoiceAiIncomingCallRequest
        {
            Msisdn = request.Msisdn,
            RawVoiceTranscript = request.RawVoiceTranscript,
            ActorUserId = request.ActorUserId ?? CurrentUserId,
        };
        var response = await _sender.Send(cmd, cancellationToken);
        return Ok(new ApiSuccessResult<SimulateVoiceAiIncomingCallResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(SimulateVoiceAiIncomingCallAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesManageTechnicalTickets)]
    [HttpPost("CreateTechnicalTicket")]
    public async Task<ActionResult<ApiSuccessResult<CreateTechnicalTicketResult>>> CreateTechnicalTicketAsync(
        [FromBody] CreateTechnicalTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.OpenedByUserId))
        {
            request = new CreateTechnicalTicketRequest
            {
                Msisdn = request.Msisdn,
                IssueType = request.IssueType,
                TicketCategory = request.TicketCategory,
                Priority = request.Priority,
                Notes = request.Notes,
                PayloadJson = request.PayloadJson,
                CustomerId = request.CustomerId,
                SubscriberProfileId = request.SubscriberProfileId,
                OpenedByUserId = CurrentUserId,
                CreatedByChannel = request.CreatedByChannel,
            };
        }
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateTechnicalTicketResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreateTechnicalTicketAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesResolveTechnicalTickets)]
    [HttpPost("UpdateTechnicalTicketStatus")]
    public async Task<ActionResult<ApiSuccessResult<UpdateTechnicalTicketStatusResult>>> UpdateTechnicalTicketStatusAsync(
        [FromBody] UpdateTechnicalTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new UpdateTechnicalTicketStatusRequest
        {
            TicketId = request.TicketId,
            NewStatus = request.NewStatus,
            NewPriority = request.NewPriority,
            OperatorNotesAr = request.OperatorNotesAr,
            ActorUserId = request.ActorUserId ?? CurrentUserId,
            IpAddress = request.IpAddress ?? ClientIp,
        };
        var response = await _sender.Send(cmd, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateTechnicalTicketStatusResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UpdateTechnicalTicketStatusAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesResolveTechnicalTickets)]
    [HttpPost("ResolveTechnicalTicket")]
    public async Task<ActionResult<ApiSuccessResult<ResolveTechnicalTicketResult>>> ResolveTechnicalTicketAsync(
        [FromBody] ResolveTechnicalTicketRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new ResolveTechnicalTicketRequest
        {
            Id = request.Id,
            ResolutionNotes = request.ResolutionNotes,
            ResolvedByUserId = request.ResolvedByUserId ?? CurrentUserId,
            IpAddress = request.IpAddress ?? ClientIp,
        };
        var response = await _sender.Send(cmd, cancellationToken);
        return Ok(new ApiSuccessResult<ResolveTechnicalTicketResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ResolveTechnicalTicketAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesResolveTechnicalTickets)]
    [HttpPost("ForceCbsSync")]
    public async Task<ActionResult<ApiSuccessResult<ForceCbsSyncResult>>> ForceCbsSyncAsync(
        [FromBody] ForceCbsSyncByTicketRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new ForceCbsSyncByTicketRequest
        {
            TicketId = request.TicketId,
            ActorUserId = request.ActorUserId ?? CurrentUserId,
            IpAddress = request.IpAddress ?? ClientIp,
        };
        var response = await _sender.Send(cmd, cancellationToken);
        return Ok(new ApiSuccessResult<ForceCbsSyncResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ForceCbsSyncAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesResolveTechnicalTickets)]
    [HttpGet("QueryLiveNetworkStatus")]
    public async Task<ActionResult<ApiSuccessResult<QueryLiveNetworkStatusByTicketResult>>> QueryLiveNetworkStatusAsync(
        [FromQuery] string ticketId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new QueryLiveNetworkStatusByTicketRequest { TicketId = ticketId },
            cancellationToken);
        return Ok(new ApiSuccessResult<QueryLiveNetworkStatusByTicketResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(QueryLiveNetworkStatusAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesResolveTechnicalTickets)]
    [HttpPost("EscalateToTier3")]
    public async Task<ActionResult<ApiSuccessResult<EscalateTechnicalTicketToTier3Result>>> EscalateToTier3Async(
        [FromBody] EscalateTechnicalTicketToTier3Request request,
        CancellationToken cancellationToken)
    {
        var cmd = new EscalateTechnicalTicketToTier3Request
        {
            TicketId = request.TicketId,
            EscalationNotes = request.EscalationNotes,
            ActorUserId = request.ActorUserId ?? CurrentUserId,
            IpAddress = request.IpAddress ?? ClientIp,
        };
        var response = await _sender.Send(cmd, cancellationToken);
        return Ok(new ApiSuccessResult<EscalateTechnicalTicketToTier3Result>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(EscalateToTier3Async),
            Content = response,
        });
    }

    [Authorize(Roles = $"{TelecomRoles.BackOffice},{TelecomRoles.Admin},{TelecomRoles.Showroom},{TelecomRoles.Management}")]
    [HttpPost("ExecuteTechnicalAction")]
    public async Task<ActionResult<ApiSuccessResult<ExecuteCustomer360TechnicalActionResult>>> ExecuteTechnicalActionAsync(
        [FromBody] ExecuteCustomer360TechnicalActionRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new ExecuteCustomer360TechnicalActionRequest
        {
            CustomerId = request.CustomerId,
            ActionType = request.ActionType,
            SubscriberProfileId = request.SubscriberProfileId,
            Msisdn = request.Msisdn,
            TargetProductCode = request.TargetProductCode,
            ProductOfferingId = request.ProductOfferingId,
            SimIccid = request.SimIccid,
            Notes = request.Notes,
            ActorUserId = request.ActorUserId ?? CurrentUserId,
        };
        var response = await _sender.Send(cmd, cancellationToken);
        return Ok(new ApiSuccessResult<ExecuteCustomer360TechnicalActionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ExecuteTechnicalActionAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesResolveTechnicalTickets)]
    [HttpPost("HlrResyncByMsisdn")]
    public async Task<ActionResult<ApiSuccessResult<HlrResyncByMsisdnResult>>> HlrResyncByMsisdnAsync(
        [FromBody] HlrResyncByMsisdnRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new HlrResyncByMsisdnRequest
        {
            Msisdn = request.Msisdn,
            TechnicalTicketId = request.TechnicalTicketId,
            ActorUserId = request.ActorUserId ?? CurrentUserId,
            IpAddress = request.IpAddress ?? ClientIp,
        };
        var response = await _sender.Send(cmd, cancellationToken);
        return Ok(new ApiSuccessResult<HlrResyncByMsisdnResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(HlrResyncByMsisdnAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesResolveTechnicalTickets)]
    [HttpPost("ToggleProductOfferingActive")]
    public async Task<ActionResult<ApiSuccessResult<ToggleProductOfferingActiveResult>>> ToggleProductOfferingActiveAsync(
        [FromBody] ToggleProductOfferingActiveRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new ToggleProductOfferingActiveRequest
        {
            Id = request.Id,
            UpdatedById = request.UpdatedById ?? CurrentUserId,
        };
        var response = await _sender.Send(cmd, cancellationToken);
        return Ok(new ApiSuccessResult<ToggleProductOfferingActiveResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ToggleProductOfferingActiveAsync),
            Content = response,
        });
    }

    [Authorize(Roles = $"{TelecomRoles.BackOffice},{TelecomRoles.Admin},{TelecomRoles.Management}")]
    [HttpGet("ResolveAuditDisplayNames")]
    public async Task<ActionResult<ApiSuccessResult<ResolveAuditDisplayNamesResult>>> ResolveAuditDisplayNamesAsync(
        [FromQuery] string? profileId,
        [FromQuery] string? technicalTicketId,
        [FromQuery] string? customerId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
            new ResolveAuditDisplayNamesRequest
            {
                ProfileId = profileId,
                TechnicalTicketId = technicalTicketId,
                CustomerId = customerId,
            },
            cancellationToken);
        return Ok(new ApiSuccessResult<ResolveAuditDisplayNamesResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ResolveAuditDisplayNamesAsync),
            Content = response,
        });
    }

    [Authorize(Roles = $"{TelecomRoles.BackOffice},{TelecomRoles.Admin},{TelecomRoles.Management}")]
    [HttpGet("GetBackOfficeAuditLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetBackOfficeAuditLogListResult>>> GetBackOfficeAuditLogListAsync(
        [FromQuery] GetBackOfficeAuditLogListRequest request,
        CancellationToken cancellationToken)
    {
        var auditReq = new GetBackOfficeAuditLogListRequest
        {
            ActorUserId = request.ActorUserId ?? CurrentUserId,
            SearchTerm = request.SearchTerm,
            ActionType = request.ActionType,
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Skip = request.Skip,
            Take = request.Take,
        };
        var response = await _sender.Send(auditReq, cancellationToken);
        return Ok(new ApiSuccessResult<GetBackOfficeAuditLogListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetBackOfficeAuditLogListAsync),
            Content = response,
        });
    }
}
