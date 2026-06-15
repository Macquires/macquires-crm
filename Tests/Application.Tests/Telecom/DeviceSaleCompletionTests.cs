using Application.Common.Telecom.DeviceSales;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class DeviceSaleCompletionTests
{
    [Fact]
    public async Task CompleteAsync_skips_when_not_device_sale()
    {
        var service = new DeviceSaleCompletionService(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
        var op = new TelecomOperationRequest { Kind = TelecomOperationKind.Migration, Id = "x" };

        await service.FulfillAsync(op, "user-1", CancellationToken.None);
    }
}
