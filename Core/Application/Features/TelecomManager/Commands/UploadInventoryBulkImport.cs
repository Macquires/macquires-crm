using Application.Common.BulkImport;
using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class UploadInventoryBulkImportResult
{
    public string JobId { get; init; } = "";
    public int TotalRows { get; init; }
}

public class UploadInventoryBulkImportRequest : IRequest<UploadInventoryBulkImportResult>, IRequireAnyPermission
{
    public Stream FileStream { get; init; } = Stream.Null;
    public string FileName { get; init; } = "";
    public BulkImportJobType JobType { get; init; }

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.InventoryManageAny;
}

public class UploadInventoryBulkImportValidator : AbstractValidator<UploadInventoryBulkImportRequest>
{
    public UploadInventoryBulkImportValidator()
    {
        RuleFor(x => x.FileName).NotEmpty();
        RuleFor(x => x.FileStream).Must(s => s != Stream.Null && s.CanRead);
        RuleFor(x => x.JobType).IsInEnum();
    }
}

public class UploadInventoryBulkImportHandler : IRequestHandler<UploadInventoryBulkImportRequest, UploadInventoryBulkImportResult>
{
    private readonly ICommandRepository<InventoryBulkImportJob> _jobRepository;
    private readonly ICommandRepository<InventoryBulkImportError> _errorRepository;
    private readonly IBulkImportFileStore _fileStore;
    private readonly IBulkImportProcessorResolver _processorResolver;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public UploadInventoryBulkImportHandler(
        ICommandRepository<InventoryBulkImportJob> jobRepository,
        ICommandRepository<InventoryBulkImportError> errorRepository,
        IBulkImportFileStore fileStore,
        IBulkImportProcessorResolver processorResolver,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _jobRepository = jobRepository;
        _errorRepository = errorRepository;
        _fileStore = fileStore;
        _processorResolver = processorResolver;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<UploadInventoryBulkImportResult> Handle(
        UploadInventoryBulkImportRequest request,
        CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);
        BulkImportLegacyMigrationGuard.EnsureLegacyMigrationJobType(request.JobType);

        var job = new InventoryBulkImportJob
        {
            JobStatus = InventoryBulkImportJobStatus.Pending,
            JobType = request.JobType,
            FileName = request.FileName,
            CreatedById = actorUserId,
            BranchId = _operator.BranchId,
        };

        await _jobRepository.CreateAsync(job, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        var relativePath = await _fileStore.SaveUploadAsync(job.Id, request.FileStream, request.FileName, cancellationToken);
        job.StoredFilePath = relativePath;

        var processor = _processorResolver.Resolve(request.JobType);
        var headerCells = await _fileStore.ReadHeaderColumnsAsync(relativePath, cancellationToken);
        var headerCheck = BulkImportHeaderValidator.Validate(processor.RequiredHeaders, headerCells);
        if (!headerCheck.IsValid)
        {
            job.JobStatus = InventoryBulkImportJobStatus.Failed;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.ErrorSummary = headerCheck.ErrorMessageAr;
            job.ErrorCount = 1;
            await _errorRepository.CreateAsync(new InventoryBulkImportError
            {
                JobId = job.Id,
                RowNumber = 0,
                Identifier = "HEADER",
                ErrorMessageAr = headerCheck.ErrorMessageAr,
                ErrorMessageEn = headerCheck.ErrorMessageEn,
                CreatedById = actorUserId
            }, cancellationToken);
            await _unitOfWork.SaveAsync(cancellationToken);
            throw new InvalidOperationException(headerCheck.ErrorMessageAr);
        }

        job.TotalRows = await _fileStore.CountDataRowsAsync(relativePath, cancellationToken);

        if (job.TotalRows > BulkImportLimits.MaxRowsPerJob)
        {
            throw new InvalidOperationException("الحد الأقصى 100,000 سطر لكل ملف.");
        }

        await _unitOfWork.SaveAsync(cancellationToken);

        return new UploadInventoryBulkImportResult { JobId = job.Id, TotalRows = job.TotalRows };
    }
}
