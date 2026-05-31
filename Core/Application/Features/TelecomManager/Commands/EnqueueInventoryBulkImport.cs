using System.Text.Json;
using Application.Common.BulkImport;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class EnqueueInventoryBulkImportResult
{
    public string JobId { get; init; } = "";
    public int QueuedRows { get; init; }
}

public record BulkImportRowDto(string Msisdn, string? Iccid, string? Puk1, string? Puk2, SimType SimType = SimType.Physical, string? Eid = null);

public class EnqueueInventoryBulkImportRequest : IRequest<EnqueueInventoryBulkImportResult>
{
    public List<BulkImportRowDto> Lines { get; init; } = new();
    public string? CreatedById { get; init; }
}

public class EnqueueInventoryBulkImportValidator : AbstractValidator<EnqueueInventoryBulkImportRequest>
{
    public EnqueueInventoryBulkImportValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleFor(x => x.Lines.Count)
            .LessThanOrEqualTo(BulkImportLimits.MaxInlineEnqueueRows)
            .WithMessage($"للصق أكثر من {BulkImportLimits.MaxInlineEnqueueRows} سطر استخدم رفع الملف من صفحة المراقبة.");
        RuleForEach(x => x.Lines).Must(l => !string.IsNullOrWhiteSpace(l.Msisdn));
    }
}

public class EnqueueInventoryBulkImportHandler : IRequestHandler<EnqueueInventoryBulkImportRequest, EnqueueInventoryBulkImportResult>
{
    private readonly ICommandRepository<InventoryBulkImportJob> _jobRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EnqueueInventoryBulkImportHandler(
        ICommandRepository<InventoryBulkImportJob> jobRepository,
        IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<EnqueueInventoryBulkImportResult> Handle(
        EnqueueInventoryBulkImportRequest request,
        CancellationToken cancellationToken)
    {
        foreach (var line in request.Lines)
        {
            if (!string.IsNullOrWhiteSpace(line.Iccid)
                && !IccidValidator.TryValidate(line.Iccid, out _, out var iccidError))
            {
                throw new InvalidOperationException($"ICCID غير صالح: {iccidError}");
            }
        }

        var job = new InventoryBulkImportJob
        {
            JobStatus = InventoryBulkImportJobStatus.Pending,
            JobType = BulkImportJobType.MsisdnAsset,
            FileName = "inline-paste.csv",
            TotalRows = request.Lines.Count,
            PayloadJson = JsonSerializer.Serialize(request.Lines),
            CreatedById = request.CreatedById
        };

        await _jobRepository.CreateAsync(job, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new EnqueueInventoryBulkImportResult { JobId = job.Id, QueuedRows = request.Lines.Count };
    }
}
