using Application.Common.BulkImport;
using Domain.Enums;

namespace Infrastructure.TelecomIntegrations.BulkImport;

public sealed class BulkImportProcessorResolver : IBulkImportProcessorResolver
{
    private readonly IReadOnlyDictionary<BulkImportJobType, IBulkImportJobProcessor> _map;

    public BulkImportProcessorResolver(IEnumerable<IBulkImportJobProcessor> processors)
    {
        _map = processors.ToDictionary(p => p.JobType);
    }

    public IBulkImportJobProcessor Resolve(BulkImportJobType jobType)
    {
        if (_map.TryGetValue(jobType, out var processor))
        {
            return processor;
        }

        throw new InvalidOperationException($"No bulk import processor registered for {jobType}.");
    }
}
