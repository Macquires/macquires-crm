using Application.Common.BulkImport;
using Infrastructure.TelecomIntegrations.BulkImport;

namespace Application.Tests.BulkImport;

public class BulkImportErrorSanitizerTests
{
    private readonly IBulkImportErrorSanitizer _sanitizer = new BulkImportErrorSanitizer();

    [Fact]
    public void MaskIdentifier_hides_middle_digits()
    {
        var masked = _sanitizer.MaskIdentifier("963931234567");
        Assert.EndsWith("4567", masked);
        Assert.DoesNotContain("12345", masked);
    }

    [Fact]
    public void SanitizeRowJson_strips_puk_and_national_id()
    {
        var row = new ParsedImportRow
        {
            Columns = new Dictionary<string, string>
            {
                ["msisdn"] = "963931234567",
                ["puk1"] = "12345678",
                ["nationalid"] = "1234567890",
                ["name"] = "Test User"
            }
        };

        var json = _sanitizer.SanitizeRowJson(row);
        Assert.NotNull(json);
        Assert.DoesNotContain("12345678", json);
        Assert.DoesNotContain("1234567890", json);
        Assert.Contains("Test User", json);
    }
}
