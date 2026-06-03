using Application.Common.Telecom.Termination;
using Domain.Entities;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class TerminationCompletionTests
{
    [Fact]
    public async Task NotifyAndAuditAsync_Skips_When_Not_Termination()
    {
        var service = new TerminationCompletionService(null!, null!, null!, null!);
        var op = new TelecomOperationRequest { Kind = TelecomOperationKind.Migration, Id = "x" };

        await service.NotifyAndAuditAsync(op, "0991111111", "user-1");
    }
}
