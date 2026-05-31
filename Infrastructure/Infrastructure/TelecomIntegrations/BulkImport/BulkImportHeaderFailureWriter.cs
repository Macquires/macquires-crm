using Application.Common.Audit;
using Application.Common.BulkImport;
using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.TelecomIntegrations.BulkImport;

internal static class BulkImportHeaderFailureWriter
{
    public static async Task ApplyAsync(
        InventoryBulkImportJob job,
        BulkImportHeaderValidationResult validation,
        ICommandRepository<InventoryBulkImportError> errorRepo,
        IUserAuditService audit,
        IUnitOfWork uow,
        CancellationToken cancellationToken)
    {
        job.JobStatus = InventoryBulkImportJobStatus.Failed;
        job.CompletedAtUtc = DateTime.UtcNow;
        job.ErrorSummary = validation.ErrorMessageAr;
        job.ErrorCount = 1;
        job.ProcessedRows = 0;
        job.SuccessCount = 0;

        await errorRepo.CreateAsync(new InventoryBulkImportError
        {
            JobId = job.Id,
            RowNumber = 0,
            Identifier = "HEADER",
            ErrorMessageAr = validation.ErrorMessageAr,
            ErrorMessageEn = validation.ErrorMessageEn,
            CreatedById = job.CreatedById
        }, cancellationToken);

        if (!string.IsNullOrEmpty(job.CreatedById))
        {
            await audit.LogAsync(new UserAuditLogRequest
            {
                ActorUserId = job.CreatedById,
                ActionType = UserAuditActionTypes.BulkImportStarted,
                EntityType = nameof(InventoryBulkImportJob),
                EntityId = job.Id,
                SummaryAr = $"فشل استيراد {job.FileName}: أعمدة غير مطابقة للقالب."
            }, cancellationToken);

            await audit.LogAsync(new UserAuditLogRequest
            {
                ActorUserId = job.CreatedById,
                ActionType = UserAuditActionTypes.BulkImportExecuted,
                EntityType = nameof(InventoryBulkImportJob),
                EntityId = job.Id,
                SummaryAr = validation.ErrorMessageAr
            }, cancellationToken);
        }

        await uow.SaveAsync(cancellationToken);
    }
}
