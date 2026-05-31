using Application.Common.Security;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetRolePermissionsResult
{
    public string? RoleName { get; init; }
    public IReadOnlyList<string>? PermissionKeys { get; init; }
}

public class GetRolePermissionsRequest : IRequest<GetRolePermissionsResult>
{
    public string? RoleName { get; init; }
}

public class GetRolePermissionsValidator : AbstractValidator<GetRolePermissionsRequest>
{
    public GetRolePermissionsValidator() => RuleFor(x => x.RoleName).NotEmpty();
}

public class GetRolePermissionsHandler : IRequestHandler<GetRolePermissionsRequest, GetRolePermissionsResult>
{
    private readonly IPermissionEvaluator _evaluator;

    public GetRolePermissionsHandler(IPermissionEvaluator evaluator) => _evaluator = evaluator;

    public async Task<GetRolePermissionsResult> Handle(GetRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var keys = await _evaluator.GetRolePermissionsAsync(request.RoleName ?? "", cancellationToken);
        return new GetRolePermissionsResult
        {
            RoleName = request.RoleName,
            PermissionKeys = keys,
        };
    }
}
