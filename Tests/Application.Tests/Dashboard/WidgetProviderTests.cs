using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Dashboard.Providers;
using Infrastructure.DataAccessManager.EFCore.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests.Dashboard;

public class WidgetProviderTests
{
    public WidgetProviderTests() => DashboardTestEncryption.EnsureInitialized();

    [Fact]
    public async Task PendingOperations_counts_pending_documents_and_confirmed()
    {
        await using var ctx = CreateContext();
        ctx.TelecomOperationRequest.AddRange(
            Op(TelecomOperationStatus.PendingDocuments),
            Op(TelecomOperationStatus.Confirmed),
            Op(TelecomOperationStatus.Draft),
            Op(TelecomOperationStatus.Completed));
        await ctx.SaveChangesAsync();

        var dto = await new PendingOperationsWidgetProvider(ctx).GetAsync();

        Assert.Equal(2, dto.ValueNumeric);
        Assert.Equal("warn", dto.Status);
    }

    [Fact]
    public async Task SubscriberCount_uses_customer_and_profile_totals()
    {
        await using var ctx = CreateContext();
        var a = IndividualCustomer.Create("A", "ACC1", "NID1", PostalAddress.Empty, null, null, null, null);
        var b = IndividualCustomer.Create("B", "ACC2", "NID2", PostalAddress.Empty, null, null, null, null);
        ctx.Customer.AddRange(a, b);
        await ctx.SaveChangesAsync();
        ctx.SubscriberProfile.Add(new SubscriberProfile { CustomerId = a.Id });
        await ctx.SaveChangesAsync();

        var dto = await new SubscriberCountWidgetProvider(ctx).GetAsync();

        Assert.Equal(2, dto.ValueNumeric);
        Assert.Contains("ملفات اشتراك", dto.Subtitle ?? "");
        Assert.Equal("ok", dto.Status);
    }

    private static QueryContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new QueryContext(options);
    }

    private static TelecomOperationRequest Op(TelecomOperationStatus status) =>
        new()
        {
            Number = Guid.NewGuid().ToString("N")[..8],
            SubscriberProfileId = Guid.NewGuid().ToString(),
            Status = status,
            Kind = TelecomOperationKind.NewActivation,
        };
}
