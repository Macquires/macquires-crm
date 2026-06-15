using Application.Tests.TestSupport;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.DataAccessManager.EFCore;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Infrastructure;

[Trait("Category", "Smoke")]
public sealed class DataContextSmokeTests
{
    [Fact]
    public void DesignTimeFactory_CreatesDataContext()
    {
        using var context = new DataContextDesignTimeFactory().CreateDbContext([]);
        Assert.NotNull(context);
    }

    [Fact]
    public void DesignTimeFactory_CreatesQueryContext()
    {
        using var context = new QueryContextDesignTimeFactory().CreateDbContext([]);
        Assert.NotNull(context);
    }

    [Fact]
    public void DesignTimeFactory_CreatesCommandContext()
    {
        using var context = new CommandContextDesignTimeFactory().CreateDbContext([]);
        Assert.NotNull(context);
    }

    [Fact]
    public async Task BranchQueryFilter_ScopesToOperatorBranch_WhenNotPrivileged()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var seedContext = new DataContext(options, TestOperatorContext.Instance))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.MsisdnAsset.AddRange(
                new MsisdnAsset { Msisdn = "0931111111", BranchId = "branch-a", PoolStatus = MsisdnPoolStatus.Available },
                new MsisdnAsset { Msisdn = "0932222222", BranchId = "branch-b", PoolStatus = MsisdnPoolStatus.Available });
            await seedContext.SaveChangesAsync();
        }

        await using var branchContext = new DataContext(options, TestOperatorContext.ForBranch("branch-a"));
        var visible = await branchContext.MsisdnAsset
            .AsNoTracking()
            .Select(m => m.Msisdn)
            .ToListAsync();

        Assert.Single(visible);
        Assert.Equal("0931111111", visible[0]);
    }

    [Fact]
    public async Task BranchQueryFilter_ScopesBulkImportJobs_ToOperatorBranch()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var seedContext = new DataContext(options, TestOperatorContext.Instance))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.InventoryBulkImportJob.AddRange(
                new InventoryBulkImportJob { FileName = "a.csv", BranchId = "branch-a" },
                new InventoryBulkImportJob { FileName = "b.csv", BranchId = "branch-b" });
            await seedContext.SaveChangesAsync();
        }

        await using var branchContext = new DataContext(options, TestOperatorContext.ForBranch("branch-a"));
        var visible = await branchContext.InventoryBulkImportJob
            .AsNoTracking()
            .Select(j => j.FileName)
            .ToListAsync();

        Assert.Single(visible);
        Assert.Equal("a.csv", visible[0]);
    }

    [Fact]
    public async Task BranchQueryFilter_ScopesSimInventory_ToOperatorBranch()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var seedContext = new DataContext(options, TestOperatorContext.Instance))
        {
            await seedContext.Database.EnsureCreatedAsync();
            var simA = SimInventory.Create("8933000000000000001");
            simA.BranchId = "branch-a";
            var simB = SimInventory.Create("8933000000000000002");
            simB.BranchId = "branch-b";
            seedContext.SimInventory.AddRange(simA, simB);
            await seedContext.SaveChangesAsync();
        }

        await using var branchContext = new DataContext(options, TestOperatorContext.ForBranch("branch-a"));
        var visible = await branchContext.SimInventory
            .AsNoTracking()
            .Select(s => s.Iccid)
            .ToListAsync();

        Assert.Single(visible);
        Assert.Equal("8933000000000000001", visible[0]);
    }

    [Fact]
    public async Task BranchQueryFilter_ScopesBillingIntegrationLogs_ToOperatorBranch()
    {
        var databaseName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (var seedContext = new DataContext(options, TestOperatorContext.Instance))
        {
            await seedContext.Database.EnsureCreatedAsync();
            seedContext.BillingIntegrationLog.AddRange(
                new BillingIntegrationLog { Message = "branch-a", BranchId = "branch-a", Success = true },
                new BillingIntegrationLog { Message = "branch-b", BranchId = "branch-b", Success = true });
            await seedContext.SaveChangesAsync();
        }

        await using var branchContext = new DataContext(options, TestOperatorContext.ForBranch("branch-a"));
        var visible = await branchContext.BillingIntegrationLog
            .AsNoTracking()
            .Select(l => l.Message)
            .ToListAsync();

        Assert.Single(visible);
        Assert.Equal("branch-a", visible[0]);
    }
}
