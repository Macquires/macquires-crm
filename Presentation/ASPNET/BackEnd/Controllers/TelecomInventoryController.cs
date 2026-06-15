using Application.Features.TelecomManager.Commands;
using Application.Features.TelecomManager.Queries;
using ASPNET.BackEnd.Common.Models;
using ASPNET.BackEnd.Common.Attributes;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Domain.Enums;

using ASPNET.BackEnd.Common.Base;
using MediatR;

namespace ASPNET.BackEnd.Controllers;

[Route("api/Telecom")]
public class TelecomInventoryController : BaseApiController
{
    public TelecomInventoryController(ISender sender) : base(sender) { }

    [RequireTelecomInventoryManage]
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

    [RequireTelecomInventoryManage]
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

    [RequireBulkImportMonitor]
    [HttpGet("DownloadBulkImportTemplate")]
    public async Task<IActionResult> DownloadBulkImportTemplateAsync(
        [FromQuery] BulkImportJobType jobType,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new DownloadBulkImportTemplateRequest { JobType = jobType }, cancellationToken);
        return File(response.FileBytes, response.ContentType, response.FileName);
    }

    [RequireTelecomInventoryManage]
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
        var response = await _sender.Send(new UploadInventoryBulkImportRequest
        {
            FileStream = stream,
            FileName = file.FileName,
            JobType = jobType,
        }, cancellationToken);

        return Ok(new ApiSuccessResult<UploadInventoryBulkImportResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(UploadInventoryBulkImportAsync),
            Content = response
        });
    }

    [RequireBulkImportMonitor]
    [HttpGet("GetInventoryBulkImportJobList")]
    public async Task<ActionResult<ApiSuccessResult<GetInventoryBulkImportJobListResult>>> GetInventoryBulkImportJobListAsync(
        CancellationToken cancellationToken,
        [FromQuery] int take = 25,
        [FromQuery] int skip = 0,
        [FromQuery] InventoryBulkImportJobStatus? status = null,
        [FromQuery] BulkImportJobType? jobType = null)
    {
        var response = await _sender.Send(new GetInventoryBulkImportJobListRequest
        {
            Take = take,
            Skip = skip,
            StatusFilter = status,
            JobTypeFilter = jobType,
        }, cancellationToken);
        return Ok(new ApiSuccessResult<GetInventoryBulkImportJobListResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetInventoryBulkImportJobListAsync),
            Content = response
        });
    }

    [RequireBulkImportMonitor]
    [HttpGet("GetInventoryBulkImportJobErrors")]
    public async Task<ActionResult<ApiSuccessResult<GetInventoryBulkImportJobErrorsResult>>> GetInventoryBulkImportJobErrorsAsync(
        [FromQuery] string jobId,
        CancellationToken cancellationToken,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var response = await _sender.Send(new GetInventoryBulkImportJobErrorsRequest
        {
            JobId = jobId,
            Skip = skip,
            Take = take,
        }, cancellationToken);
        return Ok(new ApiSuccessResult<GetInventoryBulkImportJobErrorsResult>
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(GetInventoryBulkImportJobErrorsAsync),
            Content = response
        });
    }

    [RequireBulkImportMonitor]
    [HttpGet("ExportInventoryBulkImportErrors")]
    public async Task<IActionResult> ExportInventoryBulkImportErrorsAsync(
        [FromQuery] string jobId,
        CancellationToken cancellationToken)
    {
        var response = await _sender.Send(new ExportInventoryBulkImportErrorsRequest
        {
            JobId = jobId,
        }, cancellationToken);

        if (response.FileBytes.Length == 0)
        {
            return NotFound();
        }

        return File(response.FileBytes, response.ContentType, response.FileName);
    }
}
