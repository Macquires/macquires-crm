using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomSubscriptionTypeManager.Commands;

public class CreateTelecomSubscriptionTypeResult
{
    public TelecomSubscriptionTypeLookup? Data { get; set; }
}

public class CreateTelecomSubscriptionTypeRequest : IRequest<CreateTelecomSubscriptionTypeResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => ReferenceDataPermissionSets.LineTypeManageAny;
    public string? Code { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? DisplayColor { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsDefault { get; init; }
}

public class CreateTelecomSubscriptionTypeValidator : AbstractValidator<CreateTelecomSubscriptionTypeRequest>
{
    public CreateTelecomSubscriptionTypeValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.NameAr).NotEmpty();
        RuleFor(x => x.NameEn).NotEmpty();
    }
}

public class CreateTelecomSubscriptionTypeHandler
    : IRequestHandler<CreateTelecomSubscriptionTypeRequest, CreateTelecomSubscriptionTypeResult>
{
    private readonly ICommandRepository<TelecomSubscriptionTypeLookup> _repository;
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOperatorContext _operator;

    public CreateTelecomSubscriptionTypeHandler(
        ICommandRepository<TelecomSubscriptionTypeLookup> repository,
        IQueryContext query,
        IUnitOfWork unitOfWork,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _query = query;
        _unitOfWork = unitOfWork;
        _operator = operatorContext;
    }

    public async Task<CreateTelecomSubscriptionTypeResult> Handle(
        CreateTelecomSubscriptionTypeRequest request,
        CancellationToken cancellationToken)
    {
        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        var exists = await _query.TelecomSubscriptionTypeLookup
            .AsNoTracking()
            .IsDeletedEqualTo()
            .AnyAsync(x => x.Code == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("كود النوع مستخدم مسبقاً.");
        }

        var entity = new TelecomSubscriptionTypeLookup
        {
            CreatedById = OperatorActor.RequireUserId(_operator),
            Code = code,
            NameAr = (request.NameAr ?? string.Empty).Trim(),
            NameEn = (request.NameEn ?? string.Empty).Trim(),
            DisplayColor = string.IsNullOrWhiteSpace(request.DisplayColor) ? null : request.DisplayColor.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
        };

        if (entity.IsDefault)
        {
            await _query.TelecomSubscriptionTypeLookup
                .Where(x => x.IsDefault && !x.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false), cancellationToken);
        }

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateTelecomSubscriptionTypeResult { Data = entity };
    }
}
