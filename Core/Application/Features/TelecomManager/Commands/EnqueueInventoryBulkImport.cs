using Application.Common.BulkImport;
using Application.Common.Exceptions;
using Application.Common.Security;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class EnqueueInventoryBulkImportResult
{
    public string JobId { get; init; } = "";
    public int QueuedRows { get; init; }
}

public record BulkImportRowDto(string Msisdn, string? Iccid, string? Puk1, string? Puk2, SimType SimType = SimType.Physical, string? Eid = null);

public class EnqueueInventoryBulkImportRequest : IRequest<EnqueueInventoryBulkImportResult>, IRequireAnyPermission
{
    public List<BulkImportRowDto> Lines { get; init; } = new();

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.InventoryManageAny;
}

public class EnqueueInventoryBulkImportValidator : AbstractValidator<EnqueueInventoryBulkImportRequest>
{
    public EnqueueInventoryBulkImportValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleFor(x => x.Lines.Count)
            .LessThanOrEqualTo(BulkImportLimits.MaxInlineEnqueueRows)
            .WithMessage($"للصق أكثر من {BulkImportLimits.MaxInlineEnqueueRows} سطر استخدم رفع الملف من صفحة المراقبة.");
        RuleForEach(x => x.Lines).Must(l => !string.IsNullOrWhiteSpace(l.Msisdn));
    }
}

public class EnqueueInventoryBulkImportHandler : IRequestHandler<EnqueueInventoryBulkImportRequest, EnqueueInventoryBulkImportResult>
{
    public Task<EnqueueInventoryBulkImportResult> Handle(
        EnqueueInventoryBulkImportRequest request,
        CancellationToken cancellationToken) =>
        throw new BusinessRuleViolationException(BulkImportLegacyMigrationGuard.InventoryRetiredMessageAr);
}
