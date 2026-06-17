using Application.Common.Security;

namespace Application.Tests.Security;

public sealed class PermissionSetsTests
{
    [Fact]
    public void CustomerPermissionSets_ViewAny_IncludesCustomerView()
    {
        Assert.Contains(PermissionCatalog.CustomerView, CustomerPermissionSets.ViewAny);
        Assert.Contains(PermissionCatalog.TelecomCustomerProvisioning, CustomerPermissionSets.ViewAny);
    }

    [Fact]
    public void CustomerPermissionSets_ManageAny_IncludesCustomerUpdate()
    {
        Assert.Contains(PermissionCatalog.CustomerUpdate, CustomerPermissionSets.ManageAny);
    }

    [Fact]
    public void FileAttachmentPermissionSets_UploadAny_IncludesTelecomProvisioning()
    {
        Assert.Contains(PermissionCatalog.TelecomCustomerProvisioning, FileAttachmentPermissionSets.UploadAny);
        Assert.Contains(PermissionCatalog.CustomerUpdate, FileAttachmentPermissionSets.UploadAny);
    }

    [Fact]
    public void FileAttachmentPermissionSets_ReadAny_IncludesCustomerView()
    {
        Assert.Contains(PermissionCatalog.CustomerView, FileAttachmentPermissionSets.ReadAny);
    }

    [Fact]
    public void BulkImportPermissionSets_MonitorAny_IncludesMonitorAndUpload()
    {
        Assert.Contains(PermissionCatalog.BulkImportMonitor, BulkImportPermissionSets.MonitorAny);
        Assert.Contains(PermissionCatalog.BulkImportUpload, BulkImportPermissionSets.MonitorAny);
    }

    [Fact]
    public void DashboardPermissionSets_ReadAny_MatchesTelecomReadSurface()
    {
        Assert.Contains(PermissionCatalog.CustomerView, DashboardPermissionSets.ReadAny);
        Assert.Contains(PermissionCatalog.TelecomReportsMis, DashboardPermissionSets.ReadAny);
    }

    [Fact]
    public void DashboardPermissionSets_AdminAny_IncludesSettingsAndUsers()
    {
        Assert.Contains(PermissionCatalog.AdminSettingsManage, DashboardPermissionSets.AdminAny);
        Assert.Contains(PermissionCatalog.AdminUsersManage, DashboardPermissionSets.AdminAny);
    }

    [Fact]
    public void BackOfficePermissionSets_TechnicalTicketManage_IncludesProvisioning()
    {
        Assert.Contains(PermissionCatalog.TelecomCustomerProvisioning, BackOfficePermissionSets.TechnicalTicketManageAny);
    }

    [Fact]
    public void ReferenceDataPermissionSets_LineTypeManage_MatchesControllerAttribute()
    {
        Assert.Contains(PermissionCatalog.TelecomCustomerProvisioning, ReferenceDataPermissionSets.LineTypeManageAny);
    }

    [Fact]
    public void IntegrationMonitorPermissionSets_MonitorAny_IncludesAdminIntegrationMonitor()
    {
        Assert.Contains(PermissionCatalog.AdminIntegrationMonitor, IntegrationMonitorPermissionSets.MonitorAny);
    }

    [Fact]
    public void IntegrationMonitorPermissionSets_MonitorAny_IncludesTelecomReportsMisForExecutiveCommandCenter()
    {
        Assert.Contains(PermissionCatalog.TelecomReportsMis, IntegrationMonitorPermissionSets.MonitorAny);
    }

    [Fact]
    public void AdminPermissionSets_AuditView_IncludesAdminAuditView()
    {
        Assert.Contains(PermissionCatalog.AdminAuditView, AdminPermissionSets.AuditViewAny);
    }

    [Fact]
    public void BackOfficePermissionSets_TechnicalTicketList_IncludesCustomerViewForCallCenter()
    {
        Assert.Contains(PermissionCatalog.CustomerView, BackOfficePermissionSets.TechnicalTicketListAny);
        Assert.Contains(PermissionCatalog.NetworkTechnicalView, BackOfficePermissionSets.TechnicalTicketListAny);
    }

    [Fact]
    public void BackOfficePermissionSets_TechnicalView_IncludesNetworkTechnicalView()
    {
        Assert.Contains(PermissionCatalog.NetworkTechnicalView, BackOfficePermissionSets.TechnicalViewAny);
    }

    [Fact]
    public void BackOfficeDashboardPermissionSets_AccessAny_MatchesNavigationRule()
    {
        Assert.Contains(PermissionCatalog.BulkImportMonitor, BackOfficeDashboardPermissionSets.AccessAny);
        Assert.Contains(PermissionCatalog.TelecomAssetManage, BackOfficeDashboardPermissionSets.AccessAny);
        Assert.Contains(PermissionCatalog.AdminSettingsManage, BackOfficeDashboardPermissionSets.AccessAny);
    }

    [Fact]
    public void OperationalKpiPermissionSets_ReadAny_IncludesMisAndBackOfficeDashboardAccess()
    {
        Assert.Contains(PermissionCatalog.TelecomReportsMis, OperationalKpiPermissionSets.ReadAny);
        Assert.Contains(PermissionCatalog.BulkImportMonitor, OperationalKpiPermissionSets.ReadAny);
        Assert.Contains(PermissionCatalog.TelecomAssetManage, OperationalKpiPermissionSets.ReadAny);
    }

    [Fact]
    public void PermissionScopeRules_NationalDataScope_IncludesMisAndAdmin()
    {
        Assert.Contains(PermissionCatalog.TelecomReportsMis, PermissionScopeRules.NationalDataScopeAny);
        Assert.Contains(PermissionCatalog.AdminSettingsManage, PermissionScopeRules.NationalDataScopeAny);
    }
}
