using System.Security.Claims;
using Application.Common.Integrations;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using HlrLiveStatusResult = Application.Common.Integrations.HlrLiveStatusResult;
using Application.Features.TelecomManager.Commands;
using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Base;
using ASPNET.BackEnd.Common.Models;
using Infrastructure.SecurityManager.Roles;
using Application.Common.CQS.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ASPNET.BackEnd.Controllers;

[Route("api/[controller]")]
public class TelecomController : BaseApiController
{
    private readonly IChargingSystemIntegration _charging;
    private readonly ISmsGatewayIntegration _sms;
    private readonly ITelecomOperationDocumentStore _operationDocuments;
    private readonly IQueryContext _query;

    public TelecomController(
        ISender sender,
        IChargingSystemIntegration charging,
        ISmsGatewayIntegration sms,
        ITelecomOperationDocumentStore operationDocuments,
        IQueryContext query) : base(sender)
    {
        _charging = charging;
        _sms = sms;
        _operationDocuments = operationDocuments;
        _query = query;
    }

    [Authorize(Roles = TelecomRoles.RolesCreateOperation)]
    [HttpPost("ReserveMsisdnForCustomer")]
    public async Task<ActionResult<ApiSuccessResult<ReserveMsisdnForCustomerResult>>> ReserveMsisdnForCustomerAsync(
        ReserveMsisdnForCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ReserveMsisdnForCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ReserveMsisdnForCustomerAsync),
            Content = response
        });
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

    [Authorize(Roles = TelecomRoles.RolesUploadDocument)]
    [HttpPost("UploadTelecomOperationIdentityDocument")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ApiSuccessResult<UploadTelecomOperationIdentityDocumentResult>>> UploadTelecomOperationIdentityDocumentAsync(
        [FromForm] string id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("ملف الهوية مطلوب.");
        }

        await using var stream = file.OpenReadStream();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _sender.Send(new UploadTelecomOperationIdentityDocumentRequest
        {
            Id = id,
            FileStream = stream,
            FileName = file.FileName,
            UpdatedById = actorId
        }, cancellationToken);

        return Ok(new ApiSuccessResult<UploadTelecomOperationIdentityDocumentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UploadTelecomOperationIdentityDocumentAsync),
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

    [Authorize(Roles = TelecomRoles.RolesCreateOperation)]
    [HttpPost("RecordSellingLinePayment")]
    public async Task<ActionResult<ApiSuccessResult<RecordSellingLinePaymentResult>>> RecordSellingLinePaymentAsync(
        RecordSellingLinePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<RecordSellingLinePaymentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(RecordSellingLinePaymentAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetDeviceInventoryList")]
    public async Task<ActionResult<ApiSuccessResult<GetDeviceInventoryListResult>>> GetDeviceInventoryListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? status = null,
        [FromQuery] string? branchId = null,
        [FromQuery] string? imeiContains = null)
    {
        var response = await _sender.Send(
            new GetDeviceInventoryListRequest { Status = status, BranchId = branchId, ImeiContains = imeiContains },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetDeviceInventoryListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetDeviceInventoryListAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetInstallmentPlanList")]
    public async Task<ActionResult<ApiSuccessResult<GetInstallmentPlanListResult>>> GetInstallmentPlanListAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetInstallmentPlanListRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetInstallmentPlanListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetInstallmentPlanListAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesCreateOperation)]
    [HttpPost("CreateDeviceInventory")]
    public async Task<ActionResult<ApiSuccessResult<CreateDeviceInventoryResult>>> CreateDeviceInventoryAsync(
        CreateDeviceInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreateDeviceInventoryResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreateDeviceInventoryAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesCreateOperation)]
    [HttpPost("RecordDeviceDownPayment")]
    public async Task<ActionResult<ApiSuccessResult<RecordDeviceDownPaymentResult>>> RecordDeviceDownPaymentAsync(
        RecordDeviceDownPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<RecordDeviceDownPaymentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(RecordDeviceDownPaymentAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesConfirmOperation)]
    [HttpPost("ApproveDeviceInstallment")]
    public async Task<ActionResult<ApiSuccessResult<ApproveDeviceInstallmentResult>>> ApproveDeviceInstallmentAsync(
        ApproveDeviceInstallmentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ApproveDeviceInstallmentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ApproveDeviceInstallmentAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetDeviceSaleKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetDeviceSaleKpisResult>>> GetDeviceSaleKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetDeviceSaleKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetDeviceSaleKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetDeviceSaleKpisAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesCreateOperation)]
    [HttpPost("ValidateVoucher")]
    public async Task<ActionResult<ApiSuccessResult<ValidateVoucherResult>>> ValidateVoucherAsync(
        ValidateVoucherRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ValidateVoucherResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ValidateVoucherAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesCreateOperation)]
    [HttpPost("CreatePaymentTransaction")]
    public async Task<ActionResult<ApiSuccessResult<CreatePaymentTransactionResult>>> CreatePaymentTransactionAsync(
        CreatePaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<CreatePaymentTransactionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(CreatePaymentTransactionAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesCreateOperation)]
    [HttpPost("ConfirmPaymentTransaction")]
    public async Task<ActionResult<ApiSuccessResult<ConfirmPaymentTransactionResult>>> ConfirmPaymentTransactionAsync(
        ConfirmPaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ConfirmPaymentTransactionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ConfirmPaymentTransactionAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReversePayment)]
    [HttpPost("ReversePaymentTransaction")]
    public async Task<ActionResult<ApiSuccessResult<ReversePaymentTransactionResult>>> ReversePaymentTransactionAsync(
        ReversePaymentTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ReversePaymentTransactionResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ReversePaymentTransactionAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetPaymentTransactionList")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentTransactionListResult>>> GetPaymentTransactionListAsync(
        [FromQuery] int take = 30,
        CancellationToken cancellationToken = default)
    {
        var response = await _sender.Send(new GetPaymentTransactionListRequest { Take = take }, cancellationToken);
        return Ok(new ApiSuccessResult<GetPaymentTransactionListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetPaymentTransactionListAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetPaymentServicesKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentServicesKpisResult>>> GetPaymentServicesKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetPaymentServicesKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetPaymentServicesKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetPaymentServicesKpisAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetPaymentTransactionDetail")]
    public async Task<ActionResult<ApiSuccessResult<GetPaymentTransactionDetailResult>>> GetPaymentTransactionDetailAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetPaymentTransactionDetailRequest { Id = id ?? "" }, cancellationToken);
        if (response is null)
        {
            return NotFound();
        }

        return Ok(new ApiSuccessResult<GetPaymentTransactionDetailResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetPaymentTransactionDetailAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetSellingLineActivationKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetSellingLineActivationKpisResult>>> GetSellingLineActivationKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetSellingLineActivationKpisRequest { FromUtc = fromUtc, ToUtc = toUtc, BranchId = branchId },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetSellingLineActivationKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetSellingLineActivationKpisAsync),
            Content = response
        });
    }

    [Authorize]
    [HttpGet("GetChangeGsmTypeKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetChangeGsmTypeKpisResult>>> GetChangeGsmTypeKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetChangeGsmTypeKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetChangeGsmTypeKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetChangeGsmTypeKpisAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetSuspensionKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetSuspensionKpisResult>>> GetSuspensionKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetSuspensionKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetSuspensionKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetSuspensionKpisAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetReconnectEligibility")]
    public async Task<ActionResult<ApiSuccessResult<GetReconnectEligibilityResult>>> GetReconnectEligibilityAsync(
        [FromQuery] string subscriberProfileId,
        [FromQuery] string msisdnAssetId,
        CancellationToken cancellationToken,
        [FromQuery] string? reconnectReason = null,
        [FromQuery] string? clearanceType = null,
        [FromQuery] string? paymentReference = null,
        [FromQuery] bool fraudClearanceConfirmed = false,
        [FromQuery] string? sourceSuspensionOperationId = null)
    {
        var response = await _sender.Send(
            new GetReconnectEligibilityRequest
            {
                SubscriberProfileId = subscriberProfileId,
                MsisdnAssetId = msisdnAssetId,
                ReconnectReason = reconnectReason,
                ClearanceType = clearanceType,
                PaymentReference = paymentReference,
                FraudClearanceConfirmed = fraudClearanceConfirmed,
                SourceSuspensionOperationId = sourceSuspensionOperationId,
            },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetReconnectEligibilityResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetReconnectEligibilityAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetReconnectKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetReconnectKpisResult>>> GetReconnectKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetReconnectKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetReconnectKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetReconnectKpisAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetRefundKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetRefundKpisResult>>> GetRefundKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetRefundKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetRefundKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetRefundKpisAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetTerminationKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetTerminationKpisResult>>> GetTerminationKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetTerminationKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetTerminationKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTerminationKpisAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetOfferSubscriptionKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetOfferSubscriptionKpisResult>>> GetOfferSubscriptionKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetOfferSubscriptionKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetOfferSubscriptionKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetOfferSubscriptionKpisAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetChangeNumberKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetChangeNumberKpisResult>>> GetChangeNumberKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetChangeNumberKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetChangeNumberKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetChangeNumberKpisAsync),
            Content = response,
        });
    }

    [Authorize]
    [HttpGet("GetSimSwapKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetSimSwapKpisResult>>> GetSimSwapKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetSimSwapKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetSimSwapKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetSimSwapKpisAsync),
            Content = response,
        });
    }

    [HttpGet("GetTakeOverOwnershipKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetTakeOverOwnershipKpisResult>>> GetTakeOverOwnershipKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetTakeOverOwnershipKpisRequest { FromUtc = fromUtc, ToUtc = toUtc },
            cancellationToken);
        return Ok(new ApiSuccessResult<GetTakeOverOwnershipKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTakeOverOwnershipKpisAsync),
            Content = response,
        });
    }

    [Authorize(Roles = TelecomRoles.RolesImportSim)]
    [HttpPost("EnqueueInventoryBulkImport")]
    public async Task<ActionResult<ApiSuccessResult<EnqueueInventoryBulkImportResult>>> EnqueueInventoryBulkImportAsync(
        EnqueueInventoryBulkImportRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<EnqueueInventoryBulkImportResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(EnqueueInventoryBulkImportAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesImportSim)]
    [HttpGet("GetInventoryBulkImportJobStatus")]
    public async Task<ActionResult<ApiSuccessResult<GetInventoryBulkImportJobStatusResult>>> GetInventoryBulkImportJobStatusAsync(
        [FromQuery] string jobId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetInventoryBulkImportJobStatusRequest { JobId = jobId }, cancellationToken);
        return Ok(new ApiSuccessResult<GetInventoryBulkImportJobStatusResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetInventoryBulkImportJobStatusAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesViewBulkImportMonitor)]
    [HttpGet("DownloadBulkImportTemplate")]
    public async Task<IActionResult> DownloadBulkImportTemplateAsync(
        [FromQuery] BulkImportJobType jobType,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new DownloadBulkImportTemplateRequest { JobType = jobType }, cancellationToken);
        return File(response.FileBytes, response.ContentType, response.FileName);
    }

    [Authorize(Roles = TelecomRoles.RolesImportSim)]
    [HttpPost("UploadInventoryBulkImport")]
    public async Task<ActionResult<ApiSuccessResult<UploadInventoryBulkImportResult>>> UploadInventoryBulkImportAsync(
        IFormFile file,
        [FromForm] BulkImportJobType jobType,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("الملف مطلوب.");
        }

        await using var stream = file.OpenReadStream();
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _sender.Send(new UploadInventoryBulkImportRequest
        {
            FileStream = stream,
            FileName = file.FileName,
            JobType = jobType,
            CreatedById = actorId
        }, cancellationToken);

        return Ok(new ApiSuccessResult<UploadInventoryBulkImportResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UploadInventoryBulkImportAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesViewBulkImportMonitor)]
    [HttpGet("GetInventoryBulkImportJobList")]
    public async Task<ActionResult<ApiSuccessResult<GetInventoryBulkImportJobListResult>>> GetInventoryBulkImportJobListAsync(
        CancellationToken cancellationToken,
        [FromQuery] int take = 25,
        [FromQuery] int skip = 0,
        [FromQuery] InventoryBulkImportJobStatus? status = null,
        [FromQuery] BulkImportJobType? jobType = null)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _sender.Send(new GetInventoryBulkImportJobListRequest
        {
            Take = take,
            Skip = skip,
            StatusFilter = status,
            JobTypeFilter = jobType,
            ActorUserId = actorId
        }, cancellationToken);
        return Ok(new ApiSuccessResult<GetInventoryBulkImportJobListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetInventoryBulkImportJobListAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesViewBulkImportMonitor)]
    [HttpGet("GetInventoryBulkImportJobErrors")]
    public async Task<ActionResult<ApiSuccessResult<GetInventoryBulkImportJobErrorsResult>>> GetInventoryBulkImportJobErrorsAsync(
        [FromQuery] string jobId,
        CancellationToken cancellationToken,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _sender.Send(new GetInventoryBulkImportJobErrorsRequest
        {
            JobId = jobId,
            Skip = skip,
            Take = take,
            ActorUserId = actorId
        }, cancellationToken);
        return Ok(new ApiSuccessResult<GetInventoryBulkImportJobErrorsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetInventoryBulkImportJobErrorsAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesViewBulkImportMonitor)]
    [HttpGet("ExportInventoryBulkImportErrors")]
    public async Task<IActionResult> ExportInventoryBulkImportErrorsAsync(
        [FromQuery] string jobId,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _sender.Send(new ExportInventoryBulkImportErrorsRequest
        {
            JobId = jobId,
            ActorUserId = actorId
        }, cancellationToken);

        if (response.FileBytes.Length == 0)
        {
            return NotFound();
        }

        return File(response.FileBytes, response.ContentType, response.FileName);
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("QueryHlrLiveStatus")]
    public async Task<ActionResult<ApiSuccessResult<HlrLiveStatusResult>>> QueryHlrLiveStatusAsync(
        [FromQuery] string subscriberProfileId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new QueryHlrLiveStatusRequest { SubscriberProfileId = subscriberProfileId }, cancellationToken);
        return Ok(new ApiSuccessResult<HlrLiveStatusResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(QueryHlrLiveStatusAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesConfirmOperation)]
    [HttpPost("ResyncSubscriberFromHlr")]
    public async Task<ActionResult<ApiSuccessResult<HlrResyncResult>>> ResyncSubscriberFromHlrAsync(
        ResyncSubscriberFromHlrRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<HlrResyncResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ResyncSubscriberFromHlrAsync),
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

    [Authorize(Roles = TelecomRoles.RolesMutateCustomerPrimaryLine)]
    [HttpPost("UpdateCustomerPrimaryTelecomLine")]
    public async Task<ActionResult<ApiSuccessResult<UpdateCustomerPrimaryTelecomLineResult>>> UpdateCustomerPrimaryTelecomLineAsync(
        UpdateCustomerPrimaryTelecomLineRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<UpdateCustomerPrimaryTelecomLineResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UpdateCustomerPrimaryTelecomLineAsync),
            Content = response
        });
    }

    /// <summary>Creates a new <see cref="SubscriberProfile"/> plus first MSISDN line for an existing CRM customer (multi-profile / onboarding).</summary>
    [Authorize(Roles = TelecomRoles.RolesMutateCustomerPrimaryLine)]
    [HttpPost("RegisterSubscriberProfileForCustomer")]
    public async Task<ActionResult<ApiSuccessResult<RegisterSubscriberProfileForCustomerResult>>> RegisterSubscriberProfileForCustomerAsync(
        RegisterSubscriberProfileForCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<RegisterSubscriberProfileForCustomerResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(RegisterSubscriberProfileForCustomerAsync),
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
    [HttpGet("GetTelecomOperationDetail")]
    public async Task<ActionResult<ApiSuccessResult<GetTelecomOperationDetailResult>>> GetTelecomOperationDetailAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetTelecomOperationDetailRequest { Id = id }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTelecomOperationDetailResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTelecomOperationDetailAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesConfirmOperation)]
    [HttpGet("DownloadTelecomOperationIdentityDocument")]
    public async Task<IActionResult> DownloadTelecomOperationIdentityDocumentAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        var storageKey = await _query.TelecomOperationRequest
            .AsNoTracking()
            .Where(o => !o.IsDeleted && o.Id == id)
            .Select(o => o.IdentityDocumentStorageKey)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(storageKey) || !_operationDocuments.Exists(storageKey))
        {
            return NotFound();
        }

        var path = _operationDocuments.GetAbsolutePath(storageKey);
        var contentType = Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetBillingIntegrationLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetBillingIntegrationLogListResult>>> GetBillingIntegrationLogListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? telecomOperationRequestId = null,
        [FromQuery] string? operationNumber = null,
        [FromQuery] bool? success = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25,
        [FromQuery] bool isDeleted = false)
    {
        var response = await _sender.Send(new GetBillingIntegrationLogListRequest
        {
            TelecomOperationRequestId = telecomOperationRequestId,
            OperationNumber = operationNumber,
            Success = success,
            Skip = skip,
            Take = take,
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
    [HttpGet("GetIntegrationHealthStatus")]
    public async Task<ActionResult<ApiSuccessResult<GetIntegrationHealthStatusResult>>> GetIntegrationHealthStatusAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetIntegrationHealthStatusRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetIntegrationHealthStatusResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetIntegrationHealthStatusAsync),
            Content = response,
        });
    }

    [HttpGet("GetIntegrationLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetIntegrationLogListResult>>> GetIntegrationLogListAsync(
        [FromQuery] GetIntegrationLogListRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<GetIntegrationLogListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetIntegrationLogListAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetMsisdnAssetPoolList")]
    public async Task<ActionResult<ApiSuccessResult<GetMsisdnAssetPoolListResult>>> GetMsisdnAssetPoolListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] string? status = null)
    {
        var response = await _sender.Send(new GetMsisdnAssetPoolListRequest { IsDeleted = isDeleted, Status = status }, cancellationToken);
        return Ok(new ApiSuccessResult<GetMsisdnAssetPoolListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetMsisdnAssetPoolListAsync),
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
    [HttpGet("GetStrategicMetrics")]
    public async Task<ActionResult<ApiSuccessResult<GetStrategicMetricsResult>>> GetStrategicMetricsAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(new GetStrategicMetricsRequest(regionId, branchId), cancellationToken);
        return Ok(new ApiSuccessResult<GetStrategicMetricsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetStrategicMetricsAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("TelecomUnifiedSearch")]
    public async Task<ActionResult<ApiSuccessResult<TelecomUnifiedSearchResult>>> TelecomUnifiedSearchAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? nationalId = null,
        [FromQuery] string? commercialRegistrationId = null,
        [FromQuery] string? msisdn = null)
    {
        var response = await _sender.Send(new TelecomUnifiedSearchRequest
        {
            NationalId = nationalId,
            CommercialRegistrationId = commercialRegistrationId,
            Msisdn = msisdn,
        }, cancellationToken);
        return Ok(new ApiSuccessResult<TelecomUnifiedSearchResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(TelecomUnifiedSearchAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetTelecomUniversalSearch")]
    public async Task<ActionResult<ApiSuccessResult<GetTelecomUniversalSearchResult>>> GetTelecomUniversalSearchAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? term = null,
        [FromQuery] bool availableOnly = false,
        [FromQuery] bool profilesOnly = false)
    {
        var response = await _sender.Send(new GetTelecomUniversalSearchRequest { Term = term, AvailableOnly = availableOnly, ProfilesOnly = profilesOnly }, cancellationToken);
        return Ok(new ApiSuccessResult<GetTelecomUniversalSearchResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTelecomUniversalSearchAsync),
            Content = response
        });
    }

    [Authorize(Roles = TelecomRoles.RolesReadTelecom)]
    [HttpGet("GetTelecomSubscriberProfileDetail")]
    public async Task<ActionResult<ApiSuccessResult<GetTelecomSubscriberProfileDetailResult>>> GetTelecomSubscriberProfileDetailAsync(
        CancellationToken cancellationToken,
        [FromQuery] string subscriberProfileId)
    {
        var response = await _sender.Send(
            new GetTelecomSubscriberProfileDetailRequest { SubscriberProfileId = subscriberProfileId ?? string.Empty },
            cancellationToken);
        if (response == null)
        {
            return NotFound();
        }

        return Ok(new ApiSuccessResult<GetTelecomSubscriberProfileDetailResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTelecomSubscriberProfileDetailAsync),
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
