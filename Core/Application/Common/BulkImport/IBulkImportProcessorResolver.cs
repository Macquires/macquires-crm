using Domain.Enums;

namespace Application.Common.BulkImport;

public interface IBulkImportProcessorResolver
{
    IBulkImportJobProcessor Resolve(BulkImportJobType jobType);
}
