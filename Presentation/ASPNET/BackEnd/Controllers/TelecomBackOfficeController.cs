using Application.Features.TelecomBackOfficeManager.Commands;
using Application.Features.TelecomBackOfficeManager.Queries;
using ASPNET.BackEnd.Common.Attributes;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class TelecomBackOfficeController : BaseApiController
{
    public TelecomBackOfficeController(ISender sender) : base(sender)
    {
    }

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    [RequireTechnicalTicketList]
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

    [RequireTechnicalTicketList]
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

    [RequireNetworkTechnicalView]
    [HttpPost("SimulateVoiceAiIncomingCall")]
    public async Task<ActionResult<ApiSuccessResult<SimulateVoiceAiIncomingCallResult>>> SimulateVoiceAiIncomingCallAsync(
        [FromBody] SimulateVoiceAiIncomingCallRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<SimulateVoiceAiIncomingCallResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(SimulateVoiceAiIncomingCallAsync),
            Content = response,
        });
    }

    [RequireTechnicalTicketCreate]
    [HttpPost("CreateTechnicalTicket")]
    public async Task<ActionResult<ApiSuccessResult<CreateTechnicalTicketResult>>> CreateTechnicalTicketAsync(
        [FromBody] CreateTechnicalTicketRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateTechnicalTicketResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreateTechnicalTicketAsync),
            Content = response,
        });
    }

    [RequireTechnicalTicketManage]
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

    [RequireTechnicalTicketManage]
    [HttpPost("ResolveTechnicalTicket")]
    public async Task<ActionResult<ApiSuccessResult<ResolveTechnicalTicketResult>>> ResolveTechnicalTicketAsync(
        [FromBody] ResolveTechnicalTicketRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new ResolveTechnicalTicketRequest
        {
            Id = request.Id,
            ResolutionNotes = request.ResolutionNotes,
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

    [RequireNetworkTechnicalSync]
    [HttpPost("ForceCbsSync")]
    public async Task<ActionResult<ApiSuccessResult<ForceCbsSyncResult>>> ForceCbsSyncAsync(
        [FromBody] ForceCbsSyncByTicketRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new ForceCbsSyncByTicketRequest
        {
            TicketId = request.TicketId,
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

    [RequireNetworkTechnicalSync]
    [HttpPost("ForceHlrSync")]
    public async Task<ActionResult<ApiSuccessResult<ForceHlrSyncResult>>> ForceHlrSyncAsync(
        [FromBody] ForceHlrSyncByTicketRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new ForceHlrSyncByTicketRequest
        {
            TicketId = request.TicketId,
            IpAddress = request.IpAddress ?? ClientIp,
        };
        var response = await _sender.Send(cmd, cancellationToken);
        return Ok(new ApiSuccessResult<ForceHlrSyncResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ForceHlrSyncAsync),
            Content = response,
        });
    }

    [RequireNetworkTechnicalView]
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

    [RequireTechnicalTicketEscalate]
    [HttpPost("EscalateToTier3")]
    public async Task<ActionResult<ApiSuccessResult<EscalateTechnicalTicketToTier3Result>>> EscalateToTier3Async(
        [FromBody] EscalateTechnicalTicketToTier3Request request,
        CancellationToken cancellationToken)
    {
        var cmd = new EscalateTechnicalTicketToTier3Request
        {
            TicketId = request.TicketId,
            EscalationNotes = request.EscalationNotes,
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

    [RequireTelecomProvisioning]
    [HttpPost("ExecuteTechnicalAction")]
    public async Task<ActionResult<ApiSuccessResult<ExecuteCustomer360TechnicalActionResult>>> ExecuteTechnicalActionAsync(
        [FromBody] ExecuteCustomer360TechnicalActionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ExecuteCustomer360TechnicalActionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ExecuteTechnicalActionAsync),
            Content = response,
        });
    }

    [RequireNetworkTechnicalSync]
    [HttpPost("HlrResyncByMsisdn")]
    public async Task<ActionResult<ApiSuccessResult<HlrResyncByMsisdnResult>>> HlrResyncByMsisdnAsync(
        [FromBody] HlrResyncByMsisdnRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new HlrResyncByMsisdnRequest
        {
            Msisdn = request.Msisdn,
            TechnicalTicketId = request.TechnicalTicketId,
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

    [RequireProductCatalogManage]
    [HttpPost("ToggleProductOfferingActive")]
    public async Task<ActionResult<ApiSuccessResult<ToggleProductOfferingActiveResult>>> ToggleProductOfferingActiveAsync(
        [FromBody] ToggleProductOfferingActiveRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ToggleProductOfferingActiveResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ToggleProductOfferingActiveAsync),
            Content = response,
        });
    }

    [RequireBackOfficeOperations]
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

    [RequireBackOfficePendingQueueView]
    [HttpGet("GetPendingRequests")]
    public async Task<ActionResult<ApiSuccessResult<GetPendingBackOfficeOperationsResult>>> GetPendingRequestsAsync(
        [FromQuery] GetPendingBackOfficeOperationsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<GetPendingBackOfficeOperationsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetPendingRequestsAsync),
            Content = response,
        });
    }

    [RequireFinanceBdrExecute]
    [HttpPost("ApproveRequest")]
    public async Task<ActionResult<ApiSuccessResult<ApproveBackOfficeTelecomOperationResult>>> ApproveRequestAsync(
        [FromBody] ApproveBackOfficeTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ApproveBackOfficeTelecomOperationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ApproveRequestAsync),
            Content = response,
        });
    }

    [RequireFinanceBdrExecute]
    [HttpPost("RejectRequest")]
    public async Task<ActionResult<ApiSuccessResult<RejectBackOfficeTelecomOperationResult>>> RejectRequestAsync(
        [FromBody] RejectBackOfficeTelecomOperationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<RejectBackOfficeTelecomOperationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(RejectRequestAsync),
            Content = response,
        });
    }

    [RequireTechnicalTicketManage]
    [HttpPost("ClaimTicket")]
    public async Task<ActionResult<ApiSuccessResult<ClaimTicketResult>>> ClaimTicketAsync(
        [FromBody] ClaimTicketRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ClaimTicketResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ClaimTicketAsync),
            Content = response,
        });
    }

    [RequireAdminAuditView]
    [HttpGet("GetBackOfficeAuditLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetBackOfficeAuditLogListResult>>> GetBackOfficeAuditLogListAsync(
        [FromQuery] GetBackOfficeAuditLogListRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<GetBackOfficeAuditLogListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetBackOfficeAuditLogListAsync),
            Content = response,
        });
    }
}
