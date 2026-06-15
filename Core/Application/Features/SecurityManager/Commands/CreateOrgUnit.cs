using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SecurityManager.Commands;

public class CreateOrgUnitResult
{
    public OrgUnit? Data { get; init; }
}

public class CreateOrgUnitRequest : IRequest<CreateOrgUnitResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => AdminPermissionSets.UsersManageAny;
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? ParentId { get; init; }
    public string? ManagerUserId { get; init; }
    public bool? IsActive { get; init; }
}

public class CreateOrgUnitValidator : AbstractValidator<CreateOrgUnitRequest>
{
    public CreateOrgUnitValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(256);
    }
}

public class CreateOrgUnitHandler : IRequestHandler<CreateOrgUnitRequest, CreateOrgUnitResult>
{
    private readonly ICommandRepository<OrgUnit> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _query;
    private readonly ISecurityService _securityService;
    private readonly IOperatorContext _operator;

    public CreateOrgUnitHandler(
        ICommandRepository<OrgUnit> repository,
        IUnitOfWork unitOfWork,
        IQueryContext query,
        ISecurityService securityService,
        IOperatorContext operatorContext)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _query = query;
        _securityService = securityService;
        _operator = operatorContext;
    }

    public async Task<CreateOrgUnitResult> Handle(CreateOrgUnitRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ParentId)
            && !await _query.OrgUnit.AnyAsync(x => x.Id == request.ParentId && !x.IsDeleted, cancellationToken))
        {
            throw new InvalidOperationException("الوحدة الأب غير موجودة.");
        }

        await EnsureManagerExistsAsync(request.ManagerUserId, cancellationToken);

        var entity = new OrgUnit
        {
            NameAr = request.NameAr!.Trim(),
            NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? null : request.NameEn.Trim(),
            ParentId = string.IsNullOrWhiteSpace(request.ParentId) ? null : request.ParentId.Trim(),
            ManagerUserId = string.IsNullOrWhiteSpace(request.ManagerUserId) ? null : request.ManagerUserId.Trim(),
            IsActive = request.IsActive ?? true,
            CreatedById = OperatorActor.RequireUserId(_operator),
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _repository.CreateAsync(entity, cancellationToken);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new CreateOrgUnitResult { Data = entity };
    }

    private async Task EnsureManagerExistsAsync(string? managerUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(managerUserId))
        {
            return;
        }

        var users = await _securityService.GetUserListAsync(null, cancellationToken);
        if (!users.Any(u => string.Equals(u.Id, managerUserId.Trim(), StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("مدير الفرع غير موجود.");
        }
    }
}
