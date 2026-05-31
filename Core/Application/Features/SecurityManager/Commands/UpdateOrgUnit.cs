using Application.Common.CQS.Queries;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.SecurityManager.Commands;

public class UpdateOrgUnitResult
{
    public OrgUnit? Data { get; init; }
}

public class UpdateOrgUnitRequest : IRequest<UpdateOrgUnitResult>, IRequirePermission
{
    public string PermissionKey => PermissionCatalog.AdminUsersManage;
    public string? Id { get; init; }
    public string? NameAr { get; init; }
    public string? NameEn { get; init; }
    public string? ParentId { get; init; }
    public string? ManagerUserId { get; init; }
    public bool? IsActive { get; init; }
    public string? UpdatedById { get; init; }
}

public class UpdateOrgUnitValidator : AbstractValidator<UpdateOrgUnitRequest>
{
    public UpdateOrgUnitValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(256);
    }
}

public class UpdateOrgUnitHandler : IRequestHandler<UpdateOrgUnitRequest, UpdateOrgUnitResult>
{
    private readonly ICommandRepository<OrgUnit> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryContext _query;
    private readonly ISecurityService _securityService;

    public UpdateOrgUnitHandler(
        ICommandRepository<OrgUnit> repository,
        IUnitOfWork unitOfWork,
        IQueryContext query,
        ISecurityService securityService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _query = query;
        _securityService = securityService;
    }

    public async Task<UpdateOrgUnitResult> Handle(UpdateOrgUnitRequest request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetAsync(request.Id ?? string.Empty, cancellationToken);

        if (entity == null || entity.IsDeleted)
        {
            throw new InvalidOperationException("الفرع غير موجود.");
        }

        var parentId = string.IsNullOrWhiteSpace(request.ParentId) ? null : request.ParentId.Trim();
        if (!string.IsNullOrEmpty(parentId))
        {
            if (string.Equals(parentId, entity.Id, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("لا يمكن أن تكون الوحدة أباً لنفسها.");
            }

            if (!await _query.OrgUnit.AnyAsync(x => x.Id == parentId && !x.IsDeleted, cancellationToken))
            {
                throw new InvalidOperationException("الوحدة الأب غير موجودة.");
            }
        }

        await EnsureManagerExistsAsync(request.ManagerUserId, cancellationToken);

        entity.NameAr = request.NameAr!.Trim();
        entity.NameEn = string.IsNullOrWhiteSpace(request.NameEn) ? null : request.NameEn.Trim();
        entity.ParentId = parentId;
        entity.ManagerUserId = string.IsNullOrWhiteSpace(request.ManagerUserId) ? null : request.ManagerUserId.Trim();
        entity.IsActive = request.IsActive ?? entity.IsActive;
        entity.UpdatedById = request.UpdatedById;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(entity);
        await _unitOfWork.SaveAsync(cancellationToken);

        return new UpdateOrgUnitResult { Data = entity };
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
