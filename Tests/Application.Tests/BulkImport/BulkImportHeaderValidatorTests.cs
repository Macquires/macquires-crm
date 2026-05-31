using Application.Common.BulkImport;

namespace Application.Tests.BulkImport;

public class BulkImportHeaderValidatorTests
{
    [Fact]
    public void Validate_succeeds_when_headers_match_exactly()
    {
        var result = BulkImportHeaderValidator.Validate(
            BulkImportSchemas.MsisdnAssetHeaders,
            ["MSISDN", "IMSI", "ICCID", "Pin1", "Puk1"]);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_fails_when_column_missing()
    {
        var result = BulkImportHeaderValidator.Validate(
            BulkImportSchemas.MsisdnAssetHeaders,
            ["MSISDN", "IMSI", "ICCID", "Pin1"]);

        Assert.False(result.IsValid);
        Assert.Contains("Puk1", result.MissingColumns);
        Assert.Contains("Puk1", result.ErrorMessageAr);
    }

    [Fact]
    public void Validate_fails_when_unexpected_column_present()
    {
        var result = BulkImportHeaderValidator.Validate(
            BulkImportSchemas.PackageMigrationHeaders,
            ["MSISDN", "CurrentOfferCode", "NewOfferCode", "Phone"]);

        Assert.False(result.IsValid);
        Assert.Contains("Phone", result.UnexpectedColumns);
    }

    [Fact]
    public void Validate_is_case_insensitive()
    {
        var result = BulkImportHeaderValidator.Validate(
            BulkImportSchemas.CustomerProfilesHeaders,
            ["customercode", "fullnamear", "fullnameen", "nationalid", "customertype"]);

        Assert.True(result.IsValid);
    }
}
