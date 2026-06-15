using System.Net;
using ASPNET.E2E.Tests.Infrastructure;
using ASPNET.E2E.Tests.TelecomOperations;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ASPNET.E2E.Tests;

[Collection(TelecomE2ECollection.Name)]
[Trait("Category", "Integration")]
public sealed class TelecomOperationRlsTests : TelecomE2ETestBase
{
    private readonly TelecomE2EFixture _fixture;

    public TelecomOperationRlsTests(TelecomE2EFixture fixture) => _fixture = fixture;

    [SkippableTheory]
    [MemberData(nameof(TelecomOperationDefinitions.AllOperations), MemberType = typeof(TelecomOperationDefinitions))]
    public async Task Operation_CrossBranchAccess_IsDeniedByRls(TelecomOperationKind kind)
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(8));
        var ct = cts.Token;

        var adminClient = CreateClient(_fixture);
        adminClient.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(adminClient.Client, ct));

        var (operationId, _, _) = await CreateReadyOperationAsync(
            _fixture,
            adminClient,
            kind,
            $"rls-{TelecomOperationDefinitions.DisplayName(kind)}",
            ct);

        await E2ETelecomOperationSeeds.TagOperationBranchAsync(
            _fixture.AppFactory.Services,
            operationId,
            E2EAuthHelper.BranchA,
            ct);

        var branchBToken = await E2EAuthHelper.CreateShowroomBranchTokenAsync(
            _fixture.AppFactory.Services,
            E2EAuthHelper.BranchB,
            ct);

        var intruder = CreateClient(_fixture);
        intruder.UseBearerToken(branchBToken);

        var detail = await intruder.GetOperationDetailRawAsync(operationId, ct);
        Assert.True(
            detail.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden,
            $"Expected RLS deny for {kind}, got {detail.StatusCode}");

        var confirm = await intruder.ConfirmRawAsync(operationId, ct);
        Assert.True(
            confirm.StatusCode is HttpStatusCode.NotFound
                or HttpStatusCode.Forbidden
                or HttpStatusCode.BadRequest,
            $"Expected confirm block for {kind}, got {confirm.StatusCode}");
    }

    [SkippableFact]
    public async Task Operation_MissingBranchClaim_IsDeniedByDefault()
    {
        Skip.If(_fixture.DockerUnavailable, "Docker daemon not running.");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        var ct = cts.Token;

        var adminClient = CreateClient(_fixture);
        adminClient.UseBearerToken(await E2EAuthHelper.LoginAdminAsync(adminClient.Client, ct));

        var (operationId, _, _) = await CreateReadyOperationAsync(
            _fixture,
            adminClient,
            TelecomOperationKind.Termination,
            "rls-no-branch",
            ct);

        await E2ETelecomOperationSeeds.TagOperationBranchAsync(
            _fixture.AppFactory.Services,
            operationId,
            E2EAuthHelper.BranchA,
            ct);

        var noBranchToken = await E2EAuthHelper.CreateBranchScopedTokenAsync(
            _fixture.AppFactory.Services,
            [Application.Common.Security.TelecomEnterpriseRoleMatrix.RoleShowroom],
            branchId: null,
            ct);

        var intruder = CreateClient(_fixture);
        intruder.UseBearerToken(noBranchToken);

        var detail = await intruder.GetOperationDetailRawAsync(operationId, ct);
        Assert.True(
            detail.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden,
            $"Expected deny-by-default without BranchId, got {detail.StatusCode}");
    }
}
