using Application.Common.Security;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetPermissionCatalogResult
{
    public IReadOnlyList<PermissionDefinitionDto>? Data { get; init; }
}

public class GetPermissionCatalogRequest : IRequest<GetPermissionCatalogResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => AdminPermissionSets.RolesManageAny;
}

public class GetPermissionCatalogHandler : IRequestHandler<GetPermissionCatalogRequest, GetPermissionCatalogResult>
{
    public Task<GetPermissionCatalogResult> Handle(GetPermissionCatalogRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new GetPermissionCatalogResult { Data = PermissionCatalog.All });
}
