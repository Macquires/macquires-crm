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

    public static bool CanAcceptDocumentUpload(TelecomOperationRequest entity) =>
        entity.Status == TelecomOperationStatus.Draft
        || (entity.Status == TelecomOperationStatus.PendingDocuments
            && string.Equals(entity.ApprovalLevelRequired, "BackOffice", StringComparison.OrdinalIgnoreCase)
            && entity.DocumentStatus < TelecomDocumentStatus.Uploaded);
}
