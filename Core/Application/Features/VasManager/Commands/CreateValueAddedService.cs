using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Security;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.VasManager.Commands;

public class CreateValueAddedServiceResult
{
    public TelecomValueAddedService? Data { get; set; }
}

public class CreateValueAddedServiceRequest : IRequest<CreateValueAddedServiceResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.TelecomVasManage;
    public string ServiceCode { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? NameEn { get; init; }
    public string? Description { get; init; }
    public decimal MonthlyFee { get; init; }
    public bool IsActive { get; init; } = true;
    public string HlrCommandTemplate { get; init; } = "";
    public int SortOrder { get; init; }
    public string? CreatedById { get; init; }
}

public class CreateValueAddedServiceValidator : AbstractValidator<CreateValueAddedServiceRequest>
{
    public CreateValueAddedServiceValidator()
    {
        RuleFor(x => x.ServiceCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(255);
        RuleFor(x => x.HlrCommandTemplate).NotEmpty().MaximumLength(512);
        RuleFor(x => x.MonthlyFee).GreaterThanOrEqualTo(0);
    }
}

public class CreateValueAddedServiceHandler : IRequestHandler<CreateValueAddedServiceRequest, CreateValueAddedServiceResult>
{
    private readonly ICommandRepository<TelecomValueAddedService> _repository;
    private readonly IQueryContext _query;
    private readonly IUnitOfWork _unitOfWork;

    public CreateValueAddedServiceHandler(
        ICommandRepository<TelecomValueAddedService> repository,
        IQueryContext query,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _query = query;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateValueAddedServiceResult> Handle(
        CreateValueAddedServiceRequest request,
        CancellationToken cancellationToken)
    {
        var code = request.ServiceCode.Trim().ToUpperInvariant();
        var exists = await _query.TelecomValueAddedService
            .AnyAsync(x => !x.IsDeleted && x.ServiceCode == code, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"Service code '{code}' already exists.");
        }

        var entity = new TelecomValueAddedService
        {
            ServiceCode = code,
            NameAr = request.NameAr.Trim(),
            NameEn = request.NameEn?.Trim(),
            Description = request.Description?.Trim(),
            MonthlyFee = request.MonthlyFee,
            IsActive = request.IsActive,
            HlrCommandTemplate = request.HlrCommandTemplate.Trim(),
            SortOrder = request.SortOrder,
            CreatedById = request.CreatedById,
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);
        return new CreateValueAddedServiceResult { Data = entity };
    }
}
