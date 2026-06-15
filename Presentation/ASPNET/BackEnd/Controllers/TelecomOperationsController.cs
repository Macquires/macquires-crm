using Application.Features.TelecomManager.Commands;
using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Application.Common.Telecom;
using Application.Common.Integrations;
using System.Security.Claims;
using Application.Common;
using Domain.Entities;
using Domain.Enums;

using ASPNET.BackEnd.Common.Base;
using MediatR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/Telecom")]
public class TelecomOperationsController : BaseApiController
{
    private readonly IKycDocumentStorageService _kycDocumentStorage;
    private readonly ILogger<TelecomOperationsController> _logger;

    public TelecomOperationsController(
        ISender sender,
        IKycDocumentStorageService kycDocumentStorage,
        ILogger<TelecomOperationsController> logger) : base(sender)
    {
        _kycDocumentStorage = kycDocumentStorage;
        _logger = logger;
    }

    [RequireTelecomCreate]
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

    [RequireTelecomCreate]
    [HttpPost("ReleaseMsisdnReservation")]
    public async Task<ActionResult<ApiSuccessResult<ReleaseMsisdnReservationResult>>> ReleaseMsisdnReservationAsync(
        ReleaseMsisdnReservationRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<ReleaseMsisdnReservationResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(ReleaseMsisdnReservationAsync),
            Content = response
        });
    }

    [RequireTelecomCreate]
    [EnableRateLimiting("telecom-financial")]
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

    [RequireTelecomDocumentUpload]
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

    [RequireTelecomDocumentUpload]
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
        var response = await _sender.Send(new UploadTelecomOperationIdentityDocumentRequest
        {
            Id = id,
            FileStream = stream,
            FileName = file.FileName,
        }, cancellationToken);

        return Ok(new ApiSuccessResult<UploadTelecomOperationIdentityDocumentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UploadTelecomOperationIdentityDocumentAsync),
            Content = response
        });
    }

    [RequireTelecomDocumentUpload]
    [HttpPost("UploadKycDocument")]
    [RequestSizeLimit(5_242_880)]
    public async Task<ActionResult<UploadKycDocumentResponse>> UploadKycDocumentAsync(
        [FromForm] string msisdn,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        const int maxBytes = 5 * 1024 * 1024;
        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".png", ".jpg", ".jpeg"
        };

        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ToKycUploadResponse(false, TelecomUserMessages.KycFileRequired));
            }

            if (file.Length > maxBytes)
            {
                return BadRequest(ToKycUploadResponse(false, TelecomUserMessages.KycFileTooLarge));
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                return BadRequest(ToKycUploadResponse(false, TelecomUserMessages.KycFileTypeInvalid));
            }

            var msisdnValue = (msisdn ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(msisdnValue))
            {
                return BadRequest(ToKycUploadResponse(false, TelecomUserMessages.KycMsisdnRequired));
            }

            await using var stream = file.OpenReadStream();
            var documentReferenceId = await _kycDocumentStorage.StoreKycDocumentAsync(
                msisdnValue,
                file.FileName,
                stream,
                file.ContentType ?? "application/octet-stream",
                cancellationToken);

            _logger.LogInformation(
                "KYC document uploaded for MSISDN {Msisdn}, reference {DocumentReferenceId}.",
                msisdnValue,
                documentReferenceId);

            return Ok(new UploadKycDocumentResponse
            {
                Success = true,
                DocumentReferenceId = documentReferenceId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UploadKycDocument failed for MSISDN {Msisdn}.", msisdn);
            return StatusCode(StatusCodes.Status500InternalServerError, new UploadKycDocumentResponse
            {
                Success = false,
                Message = ex.Message,
                MessageAr = ex.Message,
                MessageEn = ex.Message
            });
        }
    }

    private static UploadKycDocumentResponse ToKycUploadResponse(bool success, BilingualUserMessage message) =>
        new()
        {
            Success = success,
            Message = message.ResolveForCurrentCulture(),
            MessageAr = message.Ar,
            MessageEn = message.En
        };

    [RequireTelecomConfirm]
    [EnableRateLimiting("telecom-financial")]
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

    [RequireTelecomCreate]
    [HttpPost("FetchCashierPayment")]
    public async Task<ActionResult<ApiSuccessResult<FetchCashierPaymentResult>>> FetchCashierPaymentAsync(
        FetchCashierPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(request, cancellationToken);
        return Ok(new ApiSuccessResult<FetchCashierPaymentResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(FetchCashierPaymentAsync),
            Content = response
        });
    }

    [RequireTelecomCreate]
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
}

public sealed class UploadKycDocumentResponse
{
    public bool Success { get; init; }
    public string? DocumentReferenceId { get; init; }
    public string? Message { get; init; }
    public string? MessageAr { get; init; }
    public string? MessageEn { get; init; }
}
