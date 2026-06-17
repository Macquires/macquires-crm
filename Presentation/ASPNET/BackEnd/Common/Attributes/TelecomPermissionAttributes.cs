using Application.Common.Security;

namespace ASPNET.BackEnd.Common.Attributes;

/// <summary>Showroom / back-office / call-center read paths (replaces <c>TelecomRoles.RolesReadTelecom</c>).</summary>
public sealed class RequireTelecomReadAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomReadAttribute()
        : base(
            PermissionCatalog.CustomerView,
            PermissionCatalog.TelecomCustomerProvisioning,
            PermissionCatalog.TelecomReportsMis,
            PermissionCatalog.TelecomLineRecharge,
            PermissionCatalog.TelecomAssetManage,
            PermissionCatalog.TelecomLineActivate)
    {
    }
}

public sealed class RequireTelecomCreateAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomCreateAttribute()
        : base(TelecomOperationPermissionSets.CreateAny)
    {
    }
}

public sealed class RequireTelecomConfirmAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomConfirmAttribute()
        : base(TelecomOperationPermissionSets.ConfirmAny)
    {
    }
}

public sealed class RequireTelecomDocumentUploadAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomDocumentUploadAttribute()
        : base(TelecomOperationPermissionSets.CreateAny)
    {
    }
}

public sealed class RequireTelecomInventoryManageAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomInventoryManageAttribute()
        : base(TelecomOperationPermissionSets.InventoryManageAny)
    {
    }
}

public sealed class RequireTelecomProvisioningAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomProvisioningAttribute()
        : base(TelecomOperationPermissionSets.CustomerProvisioningAny)
    {
    }
}

public sealed class RequireTelecomPaymentAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomPaymentAttribute()
        : base(TelecomOperationPermissionSets.PaymentAny)
    {
    }
}

public sealed class RequireTelecomNetworkHlrAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomNetworkHlrAttribute()
        : base(TelecomOperationPermissionSets.NetworkHlrAny)
    {
    }
}

public sealed class RequireTelecomAdminSettingsAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomAdminSettingsAttribute()
        : base(PermissionCatalog.AdminSettingsManage, PermissionCatalog.AdminUsersManage)
    {
    }
}

public sealed class RequireTelecomPaymentReverseAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomPaymentReverseAttribute()
        : base(
            PermissionCatalog.TelecomLineRecharge,
            PermissionCatalog.TelecomLineRefund,
            PermissionCatalog.FinanceBdrExecute,
            PermissionCatalog.AdminUsersManage)
    {
    }
}

public sealed class RequireTelecomLineTypeManageAttribute : HasAnyPermissionAttribute
{
    public RequireTelecomLineTypeManageAttribute()
        : base(ReferenceDataPermissionSets.LineTypeManageAny)
    {
    }
}

public sealed class RequireBulkImportMonitorAttribute : HasAnyPermissionAttribute
{
    public RequireBulkImportMonitorAttribute()
        : base(BulkImportPermissionSets.MonitorAny)
    {
    }
}

public sealed class RequireCustomerViewPiiAttribute : HasAnyPermissionAttribute
{
    public RequireCustomerViewPiiAttribute()
        : base(PermissionCatalog.CustomerViewPii, PermissionCatalog.AdminUsersManage)
    {
    }
}

public sealed class RequireCustomerViewAttribute : HasAnyPermissionAttribute
{
    public RequireCustomerViewAttribute()
        : base(CustomerPermissionSets.ViewAny)
    {
    }
}

public sealed class RequireCustomerManageAttribute : HasAnyPermissionAttribute
{
    public RequireCustomerManageAttribute()
        : base(CustomerPermissionSets.ManageAny)
    {
    }
}

public sealed class RequireProductCatalogReadAttribute : HasAnyPermissionAttribute
{
    public RequireProductCatalogReadAttribute()
        : base(ProductCatalogPermissionSets.ReadAny)
    {
    }
}

public sealed class RequireProductCatalogManageAttribute : HasAnyPermissionAttribute
{
    public RequireProductCatalogManageAttribute()
        : base(ProductCatalogPermissionSets.ManageAny)
    {
    }
}

public sealed class RequireVasCatalogManageAttribute : HasAnyPermissionAttribute
{
    public RequireVasCatalogManageAttribute()
        : base(PermissionCatalog.TelecomVasManage, PermissionCatalog.AdminSettingsManage)
    {
    }
}

public sealed class RequireVasSubscriberAttribute : HasAnyPermissionAttribute
{
    public RequireVasSubscriberAttribute()
        : base(
            PermissionCatalog.TelecomVasToggle,
            PermissionCatalog.TelecomVasManage,
            PermissionCatalog.TelecomCustomerProvisioning,
            PermissionCatalog.CustomerView)
    {
    }
}

public sealed class RequireIntegrationMonitorAttribute : HasAnyPermissionAttribute
{
    public RequireIntegrationMonitorAttribute()
        : base(IntegrationMonitorPermissionSets.MonitorAny)
    {
    }
}

public sealed class RequireOperationalKpiAttribute : HasAnyPermissionAttribute
{
    public RequireOperationalKpiAttribute()
        : base(OperationalKpiPermissionSets.ReadAny)
    {
    }
}

public sealed class RequireBackOfficeDashboardAttribute : HasAnyPermissionAttribute
{
    public RequireBackOfficeDashboardAttribute()
        : base(BackOfficeDashboardPermissionSets.AccessAny)
    {
    }
}

public sealed class RequireBackOfficeOperationsAttribute : HasAnyPermissionAttribute
{
    public RequireBackOfficeOperationsAttribute()
        : base(BackOfficePermissionSets.OperationsAny)
    {
    }
}

public sealed class RequireNetworkTechnicalViewAttribute : HasAnyPermissionAttribute
{
    public RequireNetworkTechnicalViewAttribute()
        : base(BackOfficePermissionSets.TechnicalViewAny)
    {
    }
}

public sealed class RequireTechnicalTicketListAttribute : HasAnyPermissionAttribute
{
    public RequireTechnicalTicketListAttribute()
        : base(BackOfficePermissionSets.TechnicalTicketListAny)
    {
    }
}

public sealed class RequireTechnicalTicketCreateAttribute : HasAnyPermissionAttribute
{
    public RequireTechnicalTicketCreateAttribute()
        : base(BackOfficePermissionSets.TechnicalTicketCreateAny)
    {
    }
}

public sealed class RequireTechnicalTicketManageAttribute : HasAnyPermissionAttribute
{
    public RequireTechnicalTicketManageAttribute()
        : base(BackOfficePermissionSets.TechnicalTicketManageAny)
    {
    }
}

public sealed class RequireTechnicalTicketEscalateAttribute : HasAnyPermissionAttribute
{
    public RequireTechnicalTicketEscalateAttribute()
        : base(BackOfficePermissionSets.TechnicalEscalateAny)
    {
    }
}

public sealed class RequireNetworkTechnicalSyncAttribute : HasAnyPermissionAttribute
{
    public RequireNetworkTechnicalSyncAttribute()
        : base(BackOfficePermissionSets.TechnicalSyncAny)
    {
    }
}

public sealed class RequireFinanceBdrViewAttribute : HasAnyPermissionAttribute
{
    public RequireFinanceBdrViewAttribute()
        : base(BackOfficePermissionSets.BdrViewAny)
    {
    }
}

public sealed class RequireFinanceBdrExecuteAttribute : HasAnyPermissionAttribute
{
    public RequireFinanceBdrExecuteAttribute()
        : base(BackOfficePermissionSets.BdrExecuteAny)
    {
    }
}

public sealed class RequireReferenceDataReadAttribute : HasAnyPermissionAttribute
{
    public RequireReferenceDataReadAttribute()
        : base(ReferenceDataPermissionSets.ReadAny)
    {
    }
}

public sealed class RequireReferenceDataManageAttribute : HasAnyPermissionAttribute
{
    public RequireReferenceDataManageAttribute()
        : base(ReferenceDataPermissionSets.ManageAny)
    {
    }
}

public sealed class RequireReconnectEligibilityAttribute : HasAnyPermissionAttribute
{
    public RequireReconnectEligibilityAttribute()
        : base(TelecomEligibilityPermissionSets.ReconnectAny)
    {
    }
}

public sealed class RequireBadDebtEligibilityAttribute : HasAnyPermissionAttribute
{
    public RequireBadDebtEligibilityAttribute()
        : base(TelecomEligibilityPermissionSets.BadDebtAny)
    {
    }
}

public sealed class RequireDashboardAdminAttribute : HasAnyPermissionAttribute
{
    public RequireDashboardAdminAttribute()
        : base(DashboardPermissionSets.AdminAny)
    {
    }
}

public sealed class RequireDashboardReadAttribute : HasAnyPermissionAttribute
{
    public RequireDashboardReadAttribute()
        : base(DashboardPermissionSets.ReadAny)
    {
    }
}

public sealed class RequireFileDocumentUploadAttribute : HasAnyPermissionAttribute
{
    public RequireFileDocumentUploadAttribute()
        : base(FileAttachmentPermissionSets.UploadAny)
    {
    }
}

public sealed class RequireFileDocumentReadAttribute : HasAnyPermissionAttribute
{
    public RequireFileDocumentReadAttribute()
        : base(FileAttachmentPermissionSets.ReadAny)
    {
    }
}
