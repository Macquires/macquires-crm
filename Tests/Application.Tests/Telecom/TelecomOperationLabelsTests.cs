using Application.Common.Telecom;
using Domain.Enums;
using Xunit;

namespace Application.Tests.Telecom;

public class TelecomOperationLabelsTests
{
    [Theory]
    [InlineData(TelecomOperationKind.TakeOver, "نقل ملكية")]
    [InlineData(TelecomOperationKind.ChangeGsmType, "تحويل نوع الخط CGT")]
    public void KindLabelAr_Returns_Arabic(TelecomOperationKind kind, string expected) =>
        Assert.Equal(expected, TelecomOperationLabels.KindLabelAr(kind));

    [Fact]
    public void StatusLabelAr_PendingDocuments() =>
        Assert.Equal(
            "قيد التدقيق القانوني",
            TelecomOperationLabels.StatusLabelAr(TelecomOperationStatus.PendingDocuments));
}
