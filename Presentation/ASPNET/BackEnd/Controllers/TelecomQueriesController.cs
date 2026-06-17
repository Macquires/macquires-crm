using Application.Features.TelecomManager.Commands;
using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Telecom;
using Application.Common.Integrations;

using ASPNET.BackEnd.Common.Base;
using MediatR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/Telecom")]
public class TelecomQueriesController : BaseApiController
{
    private readonly ITelecomOperationDocumentStore _operationDocuments;
    private readonly IKycDocumentStorageService _kycDocumentStorage;

    public TelecomQueriesController(
        ISender sender,
        ITelecomOperationDocumentStore operationDocuments,
        IKycDocumentStorageService kycDocumentStorage) : base(sender)
    {
        _operationDocuments = operationDocuments;
        _kycDocumentStorage = kycDocumentStorage;
    }

    [RequireTelecomRead]
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

    [RequireTelecomRead]
    [HttpGet("GetActivationChannelLabels")]
    public async Task<ActionResult<ApiSuccessResult<GetActivationChannelLabelsResult>>> GetActivationChannelLabelsAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetActivationChannelLabelsRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetActivationChannelLabelsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetActivationChannelLabelsAsync),
            Content = response
        });
    }

    [RequireTelecomRead]
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

    [RequireTelecomRead]
    [HttpGet("DownloadTelecomOperationIdentityDocument")]
    public async Task<IActionResult> DownloadTelecomOperationIdentityDocumentAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        var refs = await _sender.Send(new GetTelecomOperationDocumentRefsRequest { Id = id }, cancellationToken);
        var storageKey = refs.IdentityDocumentStorageKey;

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

    [RequireTelecomRead]
    [HttpGet("DownloadKycDocument")]
    public async Task<IActionResult> DownloadKycDocumentAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        var refs = await _sender.Send(new GetTelecomOperationDocumentRefsRequest { Id = id }, cancellationToken);
        var referenceId = refs.KycDocumentReferenceId;

        if (string.IsNullOrWhiteSpace(referenceId))
        {
            return NotFound();
        }

        var opened = await _kycDocumentStorage.TryOpenKycDocumentAsync(referenceId, cancellationToken);
        if (opened == null)
        {
            return NotFound();
        }

        var (stream, contentType) = opened.Value;
        return File(stream, contentType, enableRangeProcessing: true);
    }

    [RequireTelecomRead]
    [HttpGet("GetInIntegrationLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetInIntegrationLogListResult>>> GetInIntegrationLogListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? telecomOperationRequestId = null,
        [FromQuery] string? operationNumber = null,
        [FromQuery] bool? success = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25,
        [FromQuery] bool isDeleted = false)
    {
        var response = await _sender.Send(new GetInIntegrationLogListRequest
        {
            TelecomOperationRequestId = telecomOperationRequestId,
            OperationNumber = operationNumber,
            Success = success,
            Skip = skip,
            Take = take,
            IsDeleted = isDeleted
        }, cancellationToken);

        return Ok(new ApiSuccessResult<GetInIntegrationLogListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetInIntegrationLogListAsync),
            Content = response
        });
    }

    [RequireTelecomRead]
    [HttpGet("GetHlrProvisioningLogList")]
    public async Task<ActionResult<ApiSuccessResult<GetHlrProvisioningLogListResult>>> GetHlrProvisioningLogListAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? telecomOperationRequestId = null,
        [FromQuery] string? operationNumber = null,
        [FromQuery] bool? success = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 25,
        [FromQuery] bool isDeleted = false)
    {
        var response = await _sender.Send(new GetHlrProvisioningLogListRequest
        {
            TelecomOperationRequestId = telecomOperationRequestId,
            OperationNumber = operationNumber,
            Success = success,
            Skip = skip,
            Take = take,
            IsDeleted = isDeleted
        }, cancellationToken);

        return Ok(new ApiSuccessResult<GetHlrProvisioningLogListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetHlrProvisioningLogListAsync),
            Content = response
        });
    }

    [RequireIntegrationMonitor]
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

    [RequireIntegrationMonitor]
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

    [RequireTelecomRead]
    [HttpGet("GetIntegrationEnvironmentContext")]
    public async Task<ActionResult<ApiSuccessResult<GetIntegrationEnvironmentContextResult>>> GetIntegrationEnvironmentContextAsync(
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new GetIntegrationEnvironmentContextRequest(), cancellationToken);
        return Ok(new ApiSuccessResult<GetIntegrationEnvironmentContextResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetIntegrationEnvironmentContextAsync),
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

    [RequireTelecomRead]
    [HttpGet("GetMsisdnAssetPoolList")]
    public async Task<ActionResult<ApiSuccessResult<GetMsisdnAssetPoolListResult>>> GetMsisdnAssetPoolListAsync(
        CancellationToken cancellationToken,
        [FromQuery] bool isDeleted = false,
        [FromQuery] string? status = null,
        [FromQuery] string? subscriptionTypeId = null)
    {
        var response = await _sender.Send(new GetMsisdnAssetPoolListRequest
        {
            IsDeleted = isDeleted,
            Status = status,
            SubscriptionTypeId = subscriptionTypeId,
        }, cancellationToken);
        return Ok(new ApiSuccessResult<GetMsisdnAssetPoolListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetMsisdnAssetPoolListAsync),
            Content = response
        });
    }

    [RequireDashboardRead]
    [HttpGet("GetTelecomDashboardKpis")]
    public async Task<ActionResult<ApiSuccessResult<GetTelecomDashboardKpisResult>>> GetTelecomDashboardKpisAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(new GetTelecomDashboardKpisRequest(regionId, branchId), cancellationToken);
        return Ok(new ApiSuccessResult<GetTelecomDashboardKpisResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetTelecomDashboardKpisAsync),
            Content = response
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetSupervisorInterventionAudit")]
    public async Task<ActionResult<ApiSuccessResult<GetSupervisorInterventionAuditResult>>> GetSupervisorInterventionAuditAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetSupervisorInterventionAuditRequest(regionId, branchId, fromUtc, toUtc),
            cancellationToken);
        return Ok(new ApiSuccessResult<GetSupervisorInterventionAuditResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetSupervisorInterventionAuditAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetExecutiveExceptions")]
    public async Task<ActionResult<ApiSuccessResult<GetExecutiveExceptionsResult>>> GetExecutiveExceptionsAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(new GetExecutiveExceptionsRequest(regionId, branchId), cancellationToken);
        return Ok(new ApiSuccessResult<GetExecutiveExceptionsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetExecutiveExceptionsAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetExecutiveWeeklyDigest")]
    public async Task<ActionResult<ApiSuccessResult<GetExecutiveWeeklyDigestResult>>> GetExecutiveWeeklyDigestAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(new GetExecutiveWeeklyDigestRequest(regionId, branchId), cancellationToken);
        return Ok(new ApiSuccessResult<GetExecutiveWeeklyDigestResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetExecutiveWeeklyDigestAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetBranchGeoHeatmap")]
    public async Task<ActionResult<ApiSuccessResult<GetBranchGeoHeatmapResult>>> GetBranchGeoHeatmapAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(new GetBranchGeoHeatmapRequest(regionId, branchId), cancellationToken);
        return Ok(new ApiSuccessResult<GetBranchGeoHeatmapResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetBranchGeoHeatmapAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetExecutiveCommandCenterSummary")]
    public async Task<ActionResult<ApiSuccessResult<GetExecutiveCommandCenterSummaryResult>>> GetExecutiveCommandCenterSummaryAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetExecutiveCommandCenterSummaryRequest(regionId, branchId, fromUtc, toUtc),
            cancellationToken);
        return Ok(new ApiSuccessResult<GetExecutiveCommandCenterSummaryResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetExecutiveCommandCenterSummaryAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetExecutiveOperationsSnapshot")]
    public async Task<ActionResult<ApiSuccessResult<GetExecutiveOperationsSnapshotResult>>> GetExecutiveOperationsSnapshotAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var response = await _sender.Send(
            new GetExecutiveOperationsSnapshotRequest(regionId, branchId, fromUtc, toUtc),
            cancellationToken);
        return Ok(new ApiSuccessResult<GetExecutiveOperationsSnapshotResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetExecutiveOperationsSnapshotAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetWorkforcePerformanceReport")]
    public async Task<ActionResult<ApiSuccessResult<GetWorkforcePerformanceReportResult>>> GetWorkforcePerformanceReportAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] WorkforceRoleFilter? roleFilter = null)
    {
        var response = await _sender.Send(
            new GetWorkforcePerformanceReportRequest(regionId, branchId, fromUtc, toUtc, roleFilter),
            cancellationToken);
        return Ok(new ApiSuccessResult<GetWorkforcePerformanceReportResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetWorkforcePerformanceReportAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
    [HttpGet("GetEmployeePerformanceDetail")]
    public async Task<ActionResult<ApiSuccessResult<GetEmployeePerformanceDetailResult>>> GetEmployeePerformanceDetailAsync(
        CancellationToken cancellationToken,
        [FromQuery] string userId,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(
            new GetEmployeePerformanceDetailRequest(userId, regionId, branchId),
            cancellationToken);
        return Ok(new ApiSuccessResult<GetEmployeePerformanceDetailResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetEmployeePerformanceDetailAsync),
            Content = response,
        });
    }

    [RequireOperationalKpi]
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

    [RequireBackOfficeDashboard]
    [HttpGet("GetOperationalAnalyticsScope")]
    public async Task<ActionResult<ApiSuccessResult<GetOperationalAnalyticsScopeResult>>> GetOperationalAnalyticsScopeAsync(
        CancellationToken cancellationToken,
        [FromQuery] string? regionId = null,
        [FromQuery] string? branchId = null)
    {
        var response = await _sender.Send(new GetOperationalAnalyticsScopeRequest(regionId, branchId), cancellationToken);
        return Ok(new ApiSuccessResult<GetOperationalAnalyticsScopeResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetOperationalAnalyticsScopeAsync),
            Content = response
        });
    }

    [RequireTelecomRead]
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

    [RequireTelecomRead]
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

    [RequireTelecomRead]
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
}
