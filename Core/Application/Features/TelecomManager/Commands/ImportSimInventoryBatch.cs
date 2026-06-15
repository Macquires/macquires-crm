using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Commands;

public class ImportSimInventoryBatchResult
{
    public int CreatedCount { get; init; }
    public bool IsBackgroundJob { get; init; }
    public string? JobId { get; init; }
    public int? QueuedRows { get; init; }
}

public record SimInventoryImportLine(string Msisdn, string? Iccid, string? Puk1, string? Puk2);

public class ImportSimInventoryBatchRequest : IRequest<ImportSimInventoryBatchResult>, IRequireAnyPermission
{
    public List<SimInventoryImportLine> Lines { get; init; } = new();

    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.InventoryManageAny;
}

public class ImportSimInventoryBatchValidator : AbstractValidator<ImportSimInventoryBatchRequest>
{
    public ImportSimInventoryBatchValidator()
    {
        RuleFor(x => x.Lines).NotEmpty();
        RuleFor(x => x.Lines.Count).LessThanOrEqualTo(100_000).WithMessage("Maximum 100,000 rows per import.");
        RuleForEach(x => x.Lines).SetValidator(new SimInventoryImportLineValidator());
    }
}

public class SimInventoryImportLineValidator : AbstractValidator<SimInventoryImportLine>
{
    public SimInventoryImportLineValidator()
    {
        RuleFor(x => x.Msisdn).NotEmpty().MaximumLength(15);
    }
}

public class ImportSimInventoryBatchHandler : IRequestHandler<ImportSimInventoryBatchRequest, ImportSimInventoryBatchResult>
{
    private const int SyncRowLimit = 500;

    private readonly ICommandRepository<MsisdnAsset> _msisdnRepository;
    private readonly ICommandRepository<SimInventory> _simRepository;
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IOperatorContext _operator;

    public ImportSimInventoryBatchHandler(
        ICommandRepository<MsisdnAsset> msisdnRepository,
        ICommandRepository<SimInventory> simRepository,
        IQueryContext query,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        IOperatorContext operatorContext)
    {
        _msisdnRepository = msisdnRepository;
        _simRepository = simRepository;
        _query = query;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _operator = operatorContext;
    }

    public async Task<ImportSimInventoryBatchResult> Handle(ImportSimInventoryBatchRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = OperatorActor.RequireUserId(_operator);
        var branchId = OperatorActor.ResolveBranchId(_operator);

        if (request.Lines.Count > SyncRowLimit)
        {
            var enqueued = await _mediator.Send(new EnqueueInventoryBulkImportRequest
            {
                Lines = request.Lines.Select(l => new BulkImportRowDto(l.Msisdn, l.Iccid, l.Puk1, l.Puk2)).ToList()
            }, cancellationToken);

            return new ImportSimInventoryBatchResult
            {
                IsBackgroundJob = true,
                JobId = enqueued.JobId,
                QueuedRows = enqueued.QueuedRows
            };
        }

        var count = 0;
        var incomingMsisdns = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in request.Lines)
        {
            var cleanMsisdn = TelecomPhoneNormalizer.TryCanonicalSyrianMsisdn(line.Msisdn.Trim()) ?? line.Msisdn.Trim();
            if (!incomingMsisdns.Add(cleanMsisdn))
            {
                continue;
            }

            var msisdnExists = await _query.MsisdnAsset
                .AsNoTracking()
                .AnyAsync(m => m.Msisdn == cleanMsisdn && !m.IsDeleted, cancellationToken);
            if (msisdnExists)
            {
                continue;
            }

            var msisdnEntity = new MsisdnAsset
            {
                Msisdn = cleanMsisdn,
                PoolStatus = MsisdnPoolStatus.Available,
                CreatedById = actorUserId,
                BranchId = branchId,
            };
            await _msisdnRepository.CreateAsync(msisdnEntity, cancellationToken);

            if (!string.IsNullOrWhiteSpace(line.Iccid))
            {
                var iccid = line.Iccid.Trim();
                var iccidExists = await _query.SimInventory
                    .AsNoTracking()
                    .AnyAsync(s => s.Iccid == iccid && !s.IsDeleted, cancellationToken);
                if (!iccidExists)
                {
                    var sim = SimInventory.Create(iccid, pin1: null, puk1: line.Puk1, pin2: null, puk2: line.Puk2);
                    sim.CreatedById = actorUserId;
                    await _simRepository.CreateAsync(sim, cancellationToken);
                }
            }

            count++;
        }

        await _unitOfWork.SaveAsync(cancellationToken);
        return new ImportSimInventoryBatchResult { CreatedCount = count };
    }
}
