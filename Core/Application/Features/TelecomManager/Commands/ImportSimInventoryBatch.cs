using Application.Common.Repositories;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;

namespace Application.Features.TelecomManager.Commands;

public class ImportSimInventoryBatchResult
{
    public int CreatedCount { get; init; }
}

public record SimInventoryImportLine(string Msisdn, string? Iccid, string? Puk1, string? Puk2);

public class ImportSimInventoryBatchRequest : IRequest<ImportSimInventoryBatchResult>
{
    public List<SimInventoryImportLine> Lines { get; init; } = new();
    public string? CreatedById { get; init; }
}

public class ImportSimInventoryBatchValidator : AbstractValidator<ImportSimInventoryBatchRequest>
{
    public ImportSimInventoryBatchValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleFor(x => x.Lines).Must(lines => lines.Count <= 500).WithMessage("Maximum 500 SIM rows per import.");
        RuleForEach(x => x.Lines).SetValidator(new SimInventoryImportLineValidator());
    }
}

public class SimInventoryImportLineValidator : AbstractValidator<SimInventoryImportLine>
{
    public SimInventoryImportLineValidator()
    {
        RuleFor(x => x.Msisdn).NotEmpty().MaximumLength(32);
    }
}

public class ImportSimInventoryBatchHandler : IRequestHandler<ImportSimInventoryBatchRequest, ImportSimInventoryBatchResult>
{
    private readonly ICommandRepository<MsisdnAsset> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ImportSimInventoryBatchHandler(ICommandRepository<MsisdnAsset> repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ImportSimInventoryBatchResult> Handle(ImportSimInventoryBatchRequest request, CancellationToken cancellationToken)
    {
        var count = 0;
        foreach (var line in request.Lines)
        {
            var entity = new MsisdnAsset
            {
                Msisdn = line.Msisdn.Trim(),
                Iccid = string.IsNullOrWhiteSpace(line.Iccid) ? null : line.Iccid.Trim(),
                Puk1 = string.IsNullOrWhiteSpace(line.Puk1) ? null : line.Puk1.Trim(),
                Puk2 = string.IsNullOrWhiteSpace(line.Puk2) ? null : line.Puk2.Trim(),
                PoolStatus = MsisdnPoolStatus.Available,
                CreatedById = request.CreatedById
            };
            await _repository.CreateAsync(entity, cancellationToken);
            count++;
        }

        await _unitOfWork.SaveAsync(cancellationToken);
        return new ImportSimInventoryBatchResult { CreatedCount = count };
    }
}
