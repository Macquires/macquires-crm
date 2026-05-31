using Application.Common.BulkImport;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.TelecomIntegrations.BulkImport;

namespace Application.Tests.BulkImport;

public class BulkImportProcessorResolverTests
{
    private sealed class StubProcessor(BulkImportJobType type) : IBulkImportJobProcessor
    {
        public BulkImportJobType JobType => type;
        public IReadOnlyList<string> RequiredHeaders => BulkImportSchemas.MsisdnAssetHeaders;
        public string TemplateFileName => "Template_Stub.csv";

        public Task<BatchProcessResult> ProcessBatchAsync(
            InventoryBulkImportJob job,
            IReadOnlyList<ParsedImportRow> rows,
            int startRowNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult(new BatchProcessResult());
    }

    [Fact]
    public void Resolver_returns_processor_for_each_job_type()
    {
        IBulkImportJobProcessor[] processors =
        [
            new StubProcessor(BulkImportJobType.MsisdnAsset),
            new StubProcessor(BulkImportJobType.CustomerProfiles),
            new StubProcessor(BulkImportJobType.PackageMigration)
        ];
        var resolver = new BulkImportProcessorResolver(processors);

        Assert.Equal(BulkImportJobType.MsisdnAsset, resolver.Resolve(BulkImportJobType.MsisdnAsset).JobType);
        Assert.Equal(BulkImportJobType.CustomerProfiles, resolver.Resolve(BulkImportJobType.CustomerProfiles).JobType);
        Assert.Equal(BulkImportJobType.PackageMigration, resolver.Resolve(BulkImportJobType.PackageMigration).JobType);
    }
}
