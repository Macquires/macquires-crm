using Application.Common.Telecom.UniversalSearch;

namespace Application.Tests.Telecom;

public sealed class TelecomUniversalSearchTermTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    public void TryParse_ReturnsNull_WhenTermTooShort(string term)
    {
        Assert.Null(TelecomUniversalSearchTerm.TryParse(term));
    }

    [Fact]
    public void TryParse_RejectsNameOnlySearch()
    {
        Assert.Null(TelecomUniversalSearchTerm.TryParse("أحمد"));
    }

    [Fact]
    public void TryParse_DetectsSyrianMobileMsisdn()
    {
        var parsed = TelecomUniversalSearchTerm.TryParse("0935123456");
        Assert.NotNull(parsed);
        Assert.True(parsed!.SearchMsisdn);
        Assert.False(parsed.SearchNationalId);
        Assert.Equal("0935123456", parsed.CanonicalMsisdn);
    }

    [Fact]
    public void TryParse_DetectsNationalId_NotMobile()
    {
        var parsed = TelecomUniversalSearchTerm.TryParse("1234567890");
        Assert.NotNull(parsed);
        Assert.True(parsed!.SearchNationalId);
        Assert.False(parsed.SearchMsisdn);
    }

    [Fact]
    public void NormalizeIndicDigitsToAscii_ConvertsArabicIndicDigits()
    {
        var normalized = TelecomUniversalSearchTerm.NormalizeIndicDigitsToAscii("٠٩٣٥١٢٣٤٥٦");
        Assert.Equal("0935123456", normalized);
    }

    [Fact]
    public void MaskNationalId_MasksAllButLastFour()
    {
        Assert.Equal("******7890", TelecomUniversalSearchTerm.MaskNationalId("1234567890"));
    }
}
