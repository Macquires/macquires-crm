using Application.Common.Telecom.OfferSubscription;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class MigrationCompletionTests
{
    [Fact]
    public async Task NotifyAndAuditAsync_skips_non_migration_kind()
    {
        var service = new MigrationCompletionService(null!, null!, null!, null!);
        await service.NotifyAndAuditAsync(
            new TelecomOperationRequest { Kind = TelecomOperationKind.SimSwap, Id = "x" },
            "0991112233",
            "user-1");
    }
}
