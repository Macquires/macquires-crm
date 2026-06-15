using Domain.Common;
using Domain.Entities;

namespace Application.Tests.Infrastructure;

public sealed class RlsBranchIdEntityCoverageTests
{
  /// <summary>Regression guard: new branch-scoped aggregates should implement <see cref="IHasBranchId"/>.</summary>
    [Fact]
    public void KnownTelecomEntities_ImplementIHasBranchId()
    {
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            "BillingIntegrationLog",
            "DeviceInventory",
            "InventoryBulkImportJob",
            "MsisdnAsset",
            "SimInventory",
            "SubscriberProfile",
            "TelecomOperationAuditLog",
            "TelecomOperationRequest",
            "TelecomPaymentAuditLog",
            "TelecomPaymentTransaction",
            "TelecomTechnicalTicket",
        };

        var actual = typeof(Domain.Entities.BillingIntegrationLog).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true }
                        && typeof(IHasBranchId).IsAssignableFrom(t)
                        && t.Namespace?.StartsWith("Domain.Entities", StringComparison.Ordinal) == true
                        && !t.IsAbstract)
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(expected.IsSubsetOf(actual), $"Missing IHasBranchId: {string.Join(", ", expected.Except(actual))}");
        Assert.True(typeof(IHasBranchId).IsAssignableFrom(typeof(Customer)));
    }

    [Fact]
    public void InventoryBulkImportError_DoesNotImplementIHasBranchId_ScopedViaJob()
    {
        var type = typeof(Domain.Entities.InventoryBulkImportError);
        Assert.False(typeof(IHasBranchId).IsAssignableFrom(type));
    }
}
