using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Repositories;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomSubscriptionTypeManager.Commands;

public class UpdateTelecomSubscriptionTypeResult
{
    public TelecomSubscriptionTypeLookup? Data { get; set; }
}

public class UpdateTelecomSubscriptionTypeRequest : IRequest<UpdateTelecomSubscriptionTypeResult>
{
    public string? Id { get; init; }
    public string? Code { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? DisplayColor { get; init; }
    public int SortOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsDefault { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateTelecomSubscriptionTypeValidator : AbstractValidator<UpdateTelecomSubscriptionTypeRequest>
{
    public UpdateTelecomSubscriptionTypeValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.NameAr).NotEmpty();
        RuleFor(x => x.NameEn).NotEmpty();
    }
}

public class UpdateTelecomSubscriptionTypeHandler
    : IRequestHandler<UpdateTelecomSubscriptionTypeRequest, UpdateTelecomSubscriptionTypeResult>
{
    private readonly ICommandRepository<TelecomSubscriptionTypeLookup> _repository;
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTelecomSubscriptionTypeHandler(
        ICommandRepository<TelecomSubscriptionTypeLookup> repository,
        IQueryContext query,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _query = query;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateTelecomSubscriptionTypeResult> Handle(
        UpdateTelecomSubscriptionTypeRequest request,
        CancellationToken cancellationToken)
    {
        var id = (request.Id ?? string.Empty).Trim();
        var entity = await _repository.GetAsync(id, cancellationToken)
                     ?? throw new InvalidOperationException("نوع الخط غير موجود.");

        var code = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        var codeTaken = await _query.TelecomSubscriptionTypeLookup
            .AsNoTracking()
            .IsDeletedEqualTo()
            .AnyAsync(x => x.Code == code && x.Id != id, cancellationToken);
        if (codeTaken)
        {
            throw new InvalidOperationException("كود النوع مستخدم مسبقاً.");
        }

        if (!request.IsActive)
        {
            var otherActive = await _query.TelecomSubscriptionTypeLookup
                .AsNoTracking()
                .IsDeletedEqualTo()
                .AnyAsync(x => x.IsActive && x.Id != id, cancellationToken);
            if (!otherActive)
            {
                throw new InvalidOperationException("لا يمكن تعطيل آخر نوع خط نشط.");
            }
        }

        if (request.IsActive && request.IsDefault)
        {
            await _query.TelecomSubscriptionTypeLookup
                .Where(x => x.IsDefault && !x.IsDeleted && x.Id != id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false), cancellationToken);
        }

        entity.UpdatedById = request.UpdatedById;
        entity.Code = code;
        entity.NameAr = (request.NameAr ?? string.Empty).Trim();
        entity.NameEn = (request.NameEn ?? string.Empty).Trim();
        entity.DisplayColor = string.IsNullOrWhiteSpace(request.DisplayColor) ? null : request.DisplayColor.Trim();
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;
        entity.IsDefault = request.IsActive && request.IsDefault;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateTelecomSubscriptionTypeResult { Data = entity };
    }
}
