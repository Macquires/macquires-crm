using Domain.Entities;
using Domain.Enums;

namespace Application.Common.Telecom.BackOffice;

/// <summary>Maps internal <see cref="TelecomOperationStatus"/> to enterprise back-office pipeline labels.</summary>
public static class BackOfficeTelecomPipelineState
{
    public const string Draft = "Draft";
    public const string PendingBackOfficeApproval = "Pending_BackOffice_Approval";
    public const string InProgress = "In_Progress";
    public const string Executing = "Executing";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string ProvisioningError = "Provisioning_Error";
    public const string ApprovedPendingCash = "Approved_Pending_Cash";

    public static string Resolve(TelecomOperationStatus status, string? approvalLevelRequired) =>
        status switch
        {
            TelecomOperationStatus.Draft => Draft,
            TelecomOperationStatus.PendingDocuments
                when string.Equals(approvalLevelRequired, "BackOffice", StringComparison.OrdinalIgnoreCase)
                => PendingBackOfficeApproval,
            TelecomOperationStatus.Paid_Pending_BackOffice_Clearance
                when string.Equals(approvalLevelRequired, "BackOffice", StringComparison.OrdinalIgnoreCase)
                => "Paid_Pending_Audit",
            TelecomOperationStatus.Approved_Pending_Cash => ApprovedPendingCash,
            TelecomOperationStatus.In_Progress => InProgress,
            TelecomOperationStatus.Confirmed
                or TelecomOperationStatus.Provisioning
                or TelecomOperationStatus.PendingExternal => Executing,
            TelecomOperationStatus.Completed => Completed,
            TelecomOperationStatus.Failed => Failed,
            TelecomOperationStatus.ProvisioningError => ProvisioningError,
            _ => status.ToString(),
        };

    public static bool IsBackOfficeQueueCandidate(TelecomOperationRequest operation) =>
        !operation.IsDeleted
        && (operation.Status == TelecomOperationStatus.PendingDocuments || operation.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance)
        && string.Equals(operation.ApprovalLevelRequired, "BackOffice", StringComparison.OrdinalIgnoreCase);

    /// <summary>Statuses that represent a decided back-office outcome for the historical audit ledger.</summary>
    public static readonly TelecomOperationStatus[] HistoricalLedgerStatuses =
    [
        TelecomOperationStatus.Completed,
        TelecomOperationStatus.Failed,
        TelecomOperationStatus.Approved_Pending_Cash,
    ];

    public static bool IsHistoricalLedgerStatus(TelecomOperationStatus status) =>
        HistoricalLedgerStatuses.Contains(status);

    /// <summary>Statuses still belonging to the active back-office work queue.</summary>
    public static bool IsActiveBackOfficeQueueStatus(TelecomOperationStatus status) =>
        status is TelecomOperationStatus.PendingDocuments
            or TelecomOperationStatus.Paid_Pending_BackOffice_Clearance
            or TelecomOperationStatus.In_Progress;

    /// <summary>Places a branch request on the back-office active queue with a reviewable status.</summary>
    public static void ApplyBackOfficeRouting(TelecomOperationRequest entity, bool paidSettlement = false)
    {
        entity.ApprovalLevelRequired = "BackOffice";
        if (paidSettlement)
        {
            entity.Status = TelecomOperationStatus.Paid_Pending_BackOffice_Clearance;
            return;
        }

        if (entity.Status == TelecomOperationStatus.Draft)
        {
            entity.Status = TelecomOperationStatus.PendingDocuments;
        }
    }

    public static bool CanAcceptDocumentUpload(TelecomOperationRequest entity) =>
        entity.Status == TelecomOperationStatus.Draft
        || ((entity.Status == TelecomOperationStatus.PendingDocuments
             || entity.Status == TelecomOperationStatus.Paid_Pending_BackOffice_Clearance)
            && string.Equals(entity.ApprovalLevelRequired, "BackOffice", StringComparison.OrdinalIgnoreCase)
            && entity.DocumentStatus < TelecomDocumentStatus.Uploaded);

    public const string BackOfficeRejectedNotePrefix = "[BackOfficeRejected]";

    public static string? TryExtractBackOfficeRejectionReason(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        foreach (var line in notes.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(BackOfficeRejectedNotePrefix, StringComparison.Ordinal))
            {
                var reason = trimmed[BackOfficeRejectedNotePrefix.Length..].Trim();
                return string.IsNullOrWhiteSpace(reason) ? null : reason;
            }
        }

        return null;
    }
}
