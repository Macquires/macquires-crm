using System.Text.Json;
using Application.Common.Audit;
using Application.Common.BulkImport;
using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.TelecomIntegrations.BulkImport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.TelecomIntegrations;

public sealed class InventoryBulkImportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryBulkImportBackgroundService> _logger;

    public InventoryBulkImportBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<InventoryBulkImportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            await RecoverStaleJobsAsync(scope.ServiceProvider, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataContext>();
                var job = await db.InventoryBulkImportJob
                    .Where(j => !j.IsDeleted && j.JobStatus == InventoryBulkImportJobStatus.Pending)
                    .OrderBy(j => j.CreatedAtUtc)
                    .FirstOrDefaultAsync(stoppingToken);

                if (job != null)
                {
                    await ProcessJobAsync(scope.ServiceProvider, job, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory bulk import worker error.");
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task RecoverStaleJobsAsync(IServiceProvider sp, CancellationToken ct)
    {
        var options = sp.GetRequiredService<IOptions<BulkImportOptions>>().Value;
        var db = sp.GetRequiredService<DataContext>();
        var cutoff = DateTime.UtcNow.AddMinutes(-options.StaleProcessingMinutes);
        var stale = await db.InventoryBulkImportJob
            .Where(j => !j.IsDeleted
                && j.JobStatus == InventoryBulkImportJobStatus.Processing
                && j.StartedAtUtc != null
                && j.StartedAtUtc < cutoff)
            .ToListAsync(ct);

        foreach (var job in stale)
        {
            job.JobStatus = InventoryBulkImportJobStatus.Failed;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.ErrorSummary = "توقفت المعالجة بسبب إعادة تشغيل الخادم أو انتهاء المهلة.";
        }

        if (stale.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogWarning("Marked {Count} stale bulk import jobs as failed.", stale.Count);
        }
    }

    private async Task ProcessJobAsync(IServiceProvider sp, InventoryBulkImportJob job, CancellationToken ct)
    {
        var db = sp.GetRequiredService<DataContext>();
        var options = sp.GetRequiredService<IOptions<BulkImportOptions>>().Value;
        var fileStore = sp.GetRequiredService<IBulkImportFileStore>();
        var resolver = sp.GetRequiredService<IBulkImportProcessorResolver>();
        var sanitizer = sp.GetRequiredService<IBulkImportErrorSanitizer>();
        var errorRepo = sp.GetRequiredService<ICommandRepository<InventoryBulkImportError>>();
        var audit = sp.GetRequiredService<IUserAuditService>();
        var uow = sp.GetRequiredService<IUnitOfWork>();

        job.JobStatus = InventoryBulkImportJobStatus.Processing;
        job.StartedAtUtc = DateTime.UtcNow;
        job.ProcessedRows = 0;
        job.SuccessCount = 0;
        job.ErrorCount = 0;
        await db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(job.CreatedById))
        {
            await audit.LogAsync(new UserAuditLogRequest
            {
                ActorUserId = job.CreatedById,
                ActionType = UserAuditActionTypes.BulkImportStarted,
                EntityType = nameof(InventoryBulkImportJob),
                EntityId = job.Id,
                SummaryAr = $"بدء استيراد {job.FileName ?? job.Id} ({job.JobType})"
            }, ct);
        }

        try
        {
            var processor = resolver.Resolve(job.JobType);
            var batchSize = options.BatchSize > 0 ? options.BatchSize : BulkImportLimits.DefaultBatchSize;

            if (!string.IsNullOrWhiteSpace(job.StoredFilePath))
            {
                var headerCells = await fileStore.ReadHeaderColumnsAsync(job.StoredFilePath, ct);
                var headerCheck = BulkImportHeaderValidator.Validate(processor.RequiredHeaders, headerCells);
                if (!headerCheck.IsValid)
                {
                    await BulkImportHeaderFailureWriter.ApplyAsync(job, headerCheck, errorRepo, audit, uow, ct);
                    _logger.LogWarning("Bulk import job {JobId} rejected: invalid header mapping.", job.Id);
                    return;
                }

                await ProcessFromFileAsync(job, processor, fileStore, sanitizer, errorRepo, db, batchSize, ct);
            }
            else if (!string.IsNullOrWhiteSpace(job.PayloadJson))
            {
                await ProcessFromPayloadJsonAsync(job, processor, sanitizer, errorRepo, db, batchSize, ct);
            }
            else
            {
                job.JobStatus = InventoryBulkImportJobStatus.Failed;
                job.ErrorSummary = "لا يوجد ملف أو بيانات للمعالجة.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk import job {JobId} failed.", job.Id);
            job.JobStatus = InventoryBulkImportJobStatus.Failed;
            job.ErrorSummary = ex.Message;
        }

        job.CompletedAtUtc = DateTime.UtcNow;
        if (job.JobStatus == InventoryBulkImportJobStatus.Processing)
        {
            job.JobStatus = job.SuccessCount == 0
                ? InventoryBulkImportJobStatus.Failed
                : job.ErrorCount > 0
                    ? InventoryBulkImportJobStatus.PartiallySucceeded
                    : InventoryBulkImportJobStatus.Completed;
        }

        job.ErrorSummary ??= $"Processed {job.ProcessedRows}, OK {job.SuccessCount}, errors {job.ErrorCount}";
        await uow.SaveAsync(ct);

        if (!string.IsNullOrEmpty(job.CreatedById))
        {
            await audit.LogAsync(new UserAuditLogRequest
            {
                ActorUserId = job.CreatedById,
                ActionType = UserAuditActionTypes.BulkImportExecuted,
                EntityType = nameof(InventoryBulkImportJob),
                EntityId = job.Id,
                SummaryAr = $"معالجة ملف [{job.FileName ?? job.Id}] نوع [{job.JobType}]. نجاح: {job.SuccessCount}، أخطاء: {job.ErrorCount}."
            }, ct);
        }

        _logger.LogInformation("Bulk import job {JobId} finished: {Summary}", job.Id, job.ErrorSummary);
    }

    private static async Task ProcessFromFileAsync(
        InventoryBulkImportJob job,
        IBulkImportJobProcessor processor,
        IBulkImportFileStore fileStore,
        IBulkImportErrorSanitizer sanitizer,
        ICommandRepository<InventoryBulkImportError> errorRepo,
        DataContext db,
        int batchSize,
        CancellationToken ct)
    {
        var batch = new List<ParsedImportRow>(batchSize);
        var rowIndex = 2;

        await foreach (var row in fileStore.ReadRowsAsync(job.StoredFilePath!, ct))
        {
            batch.Add(row);
            if (batch.Count >= batchSize)
            {
                await ProcessBatchAsync(job, processor, sanitizer, errorRepo, db, batch, rowIndex - batch.Count, ct);
                batch.Clear();
            }

            rowIndex++;
        }

        if (batch.Count > 0)
        {
            await ProcessBatchAsync(job, processor, sanitizer, errorRepo, db, batch, rowIndex - batch.Count, ct);
        }

        if (job.TotalRows == 0)
        {
            job.TotalRows = job.ProcessedRows;
        }
    }

    private static async Task ProcessFromPayloadJsonAsync(
        InventoryBulkImportJob job,
        IBulkImportJobProcessor processor,
        IBulkImportErrorSanitizer sanitizer,
        ICommandRepository<InventoryBulkImportError> errorRepo,
        DataContext db,
        int batchSize,
        CancellationToken ct)
    {
        var legacy = JsonSerializer.Deserialize<List<LegacyMsisdnLine>>(job.PayloadJson ?? "[]") ?? [];
        job.TotalRows = legacy.Count;
        var parsed = legacy.Select(l =>
        {
            var cols = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["msisdn"] = l.Msisdn,
            };
            if (!string.IsNullOrWhiteSpace(l.Iccid))
            {
                cols["iccid"] = l.Iccid;
            }

            if (!string.IsNullOrWhiteSpace(l.Eid))
            {
                cols["eid"] = l.Eid;
            }

            cols["simtype"] = l.SimType.ToString();
            return new ParsedImportRow { Columns = cols };
        }).ToList();

        for (var offset = 0; offset < parsed.Count; offset += batchSize)
        {
            var chunk = parsed.Skip(offset).Take(batchSize).ToList();
            await ProcessBatchAsync(job, processor, sanitizer, errorRepo, db, chunk, offset + 2, ct);
        }
    }

    private static async Task ProcessBatchAsync(
        InventoryBulkImportJob job,
        IBulkImportJobProcessor processor,
        IBulkImportErrorSanitizer sanitizer,
        ICommandRepository<InventoryBulkImportError> errorRepo,
        DataContext db,
        IReadOnlyList<ParsedImportRow> batch,
        int startRowNumber,
        CancellationToken ct)
    {
        var result = await processor.ProcessBatchAsync(job, batch, startRowNumber, ct);
        job.ProcessedRows += batch.Count;
        job.SuccessCount += result.SuccessCount;
        job.ErrorCount += result.ErrorCount;

        foreach (var err in result.Errors)
        {
            await errorRepo.CreateAsync(new InventoryBulkImportError
            {
                JobId = job.Id,
                RowNumber = err.RowNumber,
                Identifier = sanitizer.MaskIdentifier(err.Identifier),
                ErrorMessageAr = err.ErrorMessageAr,
                ErrorMessageEn = err.ErrorMessageEn,
                RawRowDataJson = sanitizer.SanitizeRowJson(err.Row),
                CreatedById = job.CreatedById
            }, ct);
        }

        await db.SaveChangesAsync(ct);
    }

    private sealed class LegacyMsisdnLine
    {
        public string Msisdn { get; set; } = "";
        public string? Iccid { get; set; }
        public string? Puk1 { get; set; }
        public string? Puk2 { get; set; }
        public SimType SimType { get; set; }
        public string? Eid { get; set; }
    }
}
