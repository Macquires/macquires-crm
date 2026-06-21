using Application.Common.Behaviors;
using Application.Common.Exceptions;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Application.Features.SecurityManager.Commands;
using Application.Features.SecurityManager.Queries;
using Application.Features.TelecomBackOfficeManager.Commands;
using Application.Features.TelecomBackOfficeManager.Queries;
using Application.Features.TelecomManager.Queries;
using Application.Tests.TestSupport;
using Domain.Enums;
using MediatR;

namespace Application.Tests.Security;

public sealed class PermissionAuthorizationBehaviourTests
{
    [Fact]
    public async Task UnsecuredRequest_PassesThrough_WithoutPermissionCheck()
    {
        var evaluator = new StubPermissionEvaluator();
        var behaviour = new PermissionAuthorizationBehaviour<UnsecuredTestRequest, string>(
            TestOperatorContext.Instance,
            evaluator);

        var result = await behaviour.Handle(
            new UnsecuredTestRequest(),
            () => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", result);
        Assert.Empty(evaluator.CheckedKeys);
    }

    [Fact]
    public async Task AnyPermission_Allows_WhenUserHasSecondaryKey()
    {
        var evaluator = new StubPermissionEvaluator
        {
            Granted = [PermissionCatalog.CustomerView],
        };
        var behaviour = new PermissionAuthorizationBehaviour<CustomerContactListPermissionProbe, bool>(
            TestOperatorContext.Instance,
            evaluator);

        var result = await behaviour.Handle(
            new CustomerContactListPermissionProbe(),
            () => Task.FromResult(true),
            CancellationToken.None);

        Assert.True(result);
        Assert.Contains(PermissionCatalog.CustomerView, evaluator.CheckedKeys);
    }

    [Fact]
    public async Task AnyPermission_Throws_WhenNoKeyMatches()
    {
        var behaviour = new PermissionAuthorizationBehaviour<CustomerContactListPermissionProbe, bool>(
            TestOperatorContext.Instance,
            new StubPermissionEvaluator());

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            behaviour.Handle(
                new CustomerContactListPermissionProbe(),
                () => Task.FromResult(true),
                CancellationToken.None));
    }

    [Fact]
    public async Task KpiRequest_Allows_TelecomReportsMis()
    {
        var evaluator = new StubPermissionEvaluator
        {
            Granted = [PermissionCatalog.TelecomReportsMis],
        };
        var behaviour = new PermissionAuthorizationBehaviour<GetBadDebtKpisRequest, GetBadDebtKpisResult>(
            TestOperatorContext.Instance,
            evaluator);

        var result = await behaviour.Handle(
            new GetBadDebtKpisRequest(),
            () => Task.FromResult(new GetBadDebtKpisResult()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(PermissionCatalog.TelecomReportsMis, evaluator.CheckedKeys);
    }

    [Fact]
    public async Task KpiRequest_Allows_BackOfficeDashboardAccess()
    {
        var evaluator = new StubPermissionEvaluator
        {
            Granted = [PermissionCatalog.BulkImportMonitor],
        };
        var behaviour = new PermissionAuthorizationBehaviour<GetChangeGsmTypeKpisRequest, GetChangeGsmTypeKpisResult>(
            TestOperatorContext.Instance,
            evaluator);

        var result = await behaviour.Handle(
            new GetChangeGsmTypeKpisRequest(),
            () => Task.FromResult(new GetChangeGsmTypeKpisResult()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(PermissionCatalog.BulkImportMonitor, evaluator.CheckedKeys);
    }

    [Fact]
    public async Task PendingQueue_Allows_BackOfficeDashboardAccess()
    {
        var evaluator = new StubPermissionEvaluator
        {
            Granted = [PermissionCatalog.BulkImportMonitor],
        };
        var behaviour = new PermissionAuthorizationBehaviour<GetPendingBackOfficeOperationsRequest, GetPendingBackOfficeOperationsResult>(
            TestOperatorContext.Instance,
            evaluator);

        var result = await behaviour.Handle(
            new GetPendingBackOfficeOperationsRequest(),
            () => Task.FromResult(new GetPendingBackOfficeOperationsResult()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains(PermissionCatalog.BulkImportMonitor, evaluator.CheckedKeys);
    }

    [Fact]
    public async Task SinglePermission_Throws_WhenUserNotAuthenticated()
    {
        var behaviour = new PermissionAuthorizationBehaviour<SinglePermissionProbe, bool>(
            new UnauthenticatedOperatorContext(),
            new StubPermissionEvaluator());

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            behaviour.Handle(
                new SinglePermissionProbe(),
                () => Task.FromResult(true),
                CancellationToken.None));
    }

    [Fact]
    public async Task BulkImportMonitor_Allows_WhenUserHasUploadKey()
    {
        var evaluator = new StubPermissionEvaluator
        {
            Granted = [PermissionCatalog.BulkImportUpload],
        };
        var behaviour = new PermissionAuthorizationBehaviour<DownloadBulkImportTemplateRequest, DownloadBulkImportTemplateResult>(
            TestOperatorContext.Instance,
            evaluator);

        var result = await behaviour.Handle(
            new DownloadBulkImportTemplateRequest { JobType = BulkImportJobType.CustomerProfiles },
            () => Task.FromResult(new DownloadBulkImportTemplateResult()),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task TechnicalTicketCreate_Allows_CallCenterProvisioning()
    {
        var evaluator = new StubPermissionEvaluator
        {
            Granted = [PermissionCatalog.TelecomCustomerProvisioning],
        };
        var behaviour = new PermissionAuthorizationBehaviour<CreateTechnicalTicketRequest, CreateTechnicalTicketResult>(
            TestOperatorContext.Instance,
            evaluator);

        var result = await behaviour.Handle(
            new CreateTechnicalTicketRequest { Msisdn = "963991234567" },
            () => Task.FromResult(new CreateTechnicalTicketResult()),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetRoleList_Allows_AdminSettingsFallback()
    {
        var evaluator = new StubPermissionEvaluator
        {
            Granted = [PermissionCatalog.AdminSettingsManage],
        };
        var behaviour = new PermissionAuthorizationBehaviour<GetRoleListRequest, GetRoleListResult>(
            TestOperatorContext.Instance,
            evaluator);

        var result = await behaviour.Handle(
            new GetRoleListRequest(),
            () => Task.FromResult(new GetRoleListResult()),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateRolePermissions_PreservesRoleGrantPayload()
    {
        var evaluator = new StubPermissionEvaluator
        {
            Granted = [PermissionCatalog.AdminRolesManage],
        };
        var behaviour = new PermissionAuthorizationBehaviour<UpdateRolePermissionsRequest, UpdateRolePermissionsResult>(
            TestOperatorContext.Instance,
            evaluator);

        var request = new UpdateRolePermissionsRequest
        {
            RoleName = "TestRole",
            PermissionKeys = [PermissionCatalog.CustomerView],
        };

        var result = await behaviour.Handle(
            request,
            () => Task.FromResult(new UpdateRolePermissionsResult()),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Single(request.PermissionKeys!);
    }

    [Fact]
    public async Task AuthenticatedOperator_Passes_WithoutPermissionKey()
    {
        var behaviour = new PermissionAuthorizationBehaviour<AuthenticatedOperatorProbe, bool>(
            TestOperatorContext.Instance,
            new StubPermissionEvaluator());

        var result = await behaviour.Handle(
            new AuthenticatedOperatorProbe(),
            () => Task.FromResult(true),
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task AuthenticatedOperator_Throws_WhenNotSignedIn()
    {
        var behaviour = new PermissionAuthorizationBehaviour<AuthenticatedOperatorProbe, bool>(
            new UnauthenticatedOperatorContext(),
            new StubPermissionEvaluator());

        await Assert.ThrowsAsync<BusinessRuleViolationException>(() =>
            behaviour.Handle(
                new AuthenticatedOperatorProbe(),
                () => Task.FromResult(true),
                CancellationToken.None));
    }

    [Fact]
    public async Task IntegrationEnvironmentContext_Allows_TelecomReadSurface()
    {
        var evaluator = new StubPermissionEvaluator
        {
            Granted = [PermissionCatalog.CustomerView],
        };
        var behaviour = new PermissionAuthorizationBehaviour<GetIntegrationEnvironmentContextRequest, GetIntegrationEnvironmentContextResult>(
            TestOperatorContext.Instance,
            evaluator);

        var result = await behaviour.Handle(
            new GetIntegrationEnvironmentContextRequest(),
            () => Task.FromResult(new GetIntegrationEnvironmentContextResult()),
            CancellationToken.None);

        Assert.NotNull(result);
    }

    private sealed class AuthenticatedOperatorProbe : IRequest<bool>, IRequireAuthenticatedOperator;

    private sealed record UnsecuredTestRequest;

    private sealed class CustomerContactListPermissionProbe : IRequest<bool>, IRequireAnyPermission
    {
        public IReadOnlyList<string> PermissionKeys => CustomerPermissionSets.ViewAny;
    }

    private sealed class SinglePermissionProbe : IRequest<bool>, IRequirePermission
    {
        public string PermissionKey => PermissionCatalog.AdminSettingsManage;
    }

    private sealed class UnauthenticatedOperatorContext : IOperatorContext
    {
        public string? UserId => null;
        public IReadOnlyList<string> Roles { get; } = [];
        public IReadOnlyList<string> Permissions { get; } = [];
        public TelecomMenuPersona? EffectivePersona => null;
        public bool IsAuthenticated => false;
        public string? BranchId => null;
    }

    private sealed class StubPermissionEvaluator : IPermissionEvaluator
    {
        public HashSet<string> Granted { get; init; } = [];
        public List<string> CheckedKeys { get; } = [];

        public Task<bool> HasPermissionAsync(string userId, string permissionKey, CancellationToken cancellationToken = default)
        {
            CheckedKeys.Add(permissionKey);
            return Task.FromResult(Granted.Contains(permissionKey));
        }

        public Task<IReadOnlyList<string>> GetUserPermissionKeysAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>(Granted.ToList());

        public Task<IReadOnlyList<string>> GetUserRoleNamesAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> GetRolePermissionsAsync(string roleName, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task UpdateRolePermissionsAsync(
            string roleName,
            IReadOnlyList<string> permissionKeys,
            string? grantedById,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
