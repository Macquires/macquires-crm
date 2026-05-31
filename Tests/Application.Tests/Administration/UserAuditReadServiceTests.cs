using Application.Common.Audit;
using Application.Common.Security;
using Domain.Entities;
using Infrastructure.Audit;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Administration;

public class UserAuditReadServiceTests
{
    private sealed class TestEncryption : IFieldEncryptionService
    {
        public string Encrypt(string plain) => plain;
        public string Decrypt(string cipher) => cipher;
        public string ComputeSearchHash(string plain) => plain;
    }

    [Fact]
    public async Task QueryAsync_filters_by_action_type()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var context = new DataContext(options);
        context.UserAuditLog.AddRange(
            new UserAuditLog
            {
                ActorUserId = "a1",
                ActionType = UserAuditActionTypes.UserLoggedIn,
                OccurredAtUtc = DateTime.UtcNow,
                SummaryAr = "ok",
            },
            new UserAuditLog
            {
                ActorUserId = "a1",
                ActionType = UserAuditActionTypes.UserLoginFailed,
                OccurredAtUtc = DateTime.UtcNow,
                SummaryAr = "fail",
            });
        await context.SaveChangesAsync();

        var service = new UserAuditReadService(context, new TestEncryption());
        var result = await service.QueryAsync(
            new UserAuditLogQuery { ActionType = UserAuditActionTypes.UserLoggedIn, Take = 50 });

        Assert.Single(result.Items);
        Assert.Equal(UserAuditActionTypes.UserLoggedIn, result.Items[0].ActionType);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task QueryAsync_searchTerm_matches_summary_msisdn()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        await using var context = new DataContext(options);
        context.UserAuditLog.Add(new UserAuditLog
        {
            ActorUserId = "a1",
            ActionType = UserAuditActionTypes.NetworkCommandExecuted,
            OccurredAtUtc = DateTime.UtcNow,
            SummaryAr = "تفعيل خدمة للخط 0939000001",
        });
        context.UserAuditLog.Add(new UserAuditLog
        {
            ActorUserId = "a1",
            ActionType = UserAuditActionTypes.UserLoggedIn,
            OccurredAtUtc = DateTime.UtcNow,
            SummaryAr = "دخول",
        });
        await context.SaveChangesAsync();

        var service = new UserAuditReadService(context, new TestEncryption());
        var result = await service.QueryAsync(new UserAuditLogQuery { SearchTerm = "0939000001", Take = 50 });

        Assert.Single(result.Items);
        Assert.Contains("0939000001", result.Items[0].SummaryAr);
    }
}
