using Application.Common.BulkImport;
using Domain.Enums;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public class DownloadBulkImportTemplateResult
{
    public byte[] FileBytes { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = "template.csv";
    public string ContentType { get; init; } = "text/csv";
}

public class DownloadBulkImportTemplateRequest : IRequest<DownloadBulkImportTemplateResult>
{
    public BulkImportJobType JobType { get; init; }
}

public class DownloadBulkImportTemplateHandler : IRequestHandler<DownloadBulkImportTemplateRequest, DownloadBulkImportTemplateResult>
{
    private readonly IBulkImportProcessorResolver _resolver;

    public DownloadBulkImportTemplateHandler(IBulkImportProcessorResolver resolver) => _resolver = resolver;

    public Task<DownloadBulkImportTemplateResult> Handle(
        DownloadBulkImportTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var processor = _resolver.Resolve(request.JobType);
        var headerLine = string.Join(",", processor.RequiredHeaders);
        var utf8 = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = utf8.GetBytes(headerLine + Environment.NewLine);

        return Task.FromResult(new DownloadBulkImportTemplateResult
        {
            FileBytes = bytes,
            FileName = processor.TemplateFileName,
            ContentType = "text/csv"
        });
    }
}
