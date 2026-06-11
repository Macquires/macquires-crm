using Application.Common.Settings;
using Domain.Entities;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Application.Tests.Administration;

using Application.Tests.TestSupport;

public class GlobalSettingsProviderJwtTests
{
    [Fact]
    public async Task GetIntAsync_reads_jwt_minutes_from_database()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new DataContext(options, TestOperatorContext.Instance);
        context.GlobalSetting.Add(new GlobalSetting
        {
            Key = GlobalSettingKeys.JwtAccessTokenMinutes,
            Value = "90",
            Category = "Security",
        });
        await context.SaveChangesAsync();

        var provider = new GlobalSettingsProvider(context, new MemoryCache(new MemoryCacheOptions()));
        var minutes = await provider.GetIntAsync(GlobalSettingKeys.JwtAccessTokenMinutes, 60, 5, 1440);

        Assert.Equal(90, minutes);
    }
}
