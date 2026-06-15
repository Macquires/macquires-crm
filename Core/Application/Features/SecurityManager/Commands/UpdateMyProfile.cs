using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;

public class UpdateMyProfileResult
{
    public string? Data { get; init; }
}

public class UpdateMyProfileRequest : IRequest<UpdateMyProfileResult>, IRequireAuthenticatedOperator
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
}

public class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}

public class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileRequest, UpdateMyProfileResult>
{
    private readonly ISecurityService _securityService;
    private readonly IOperatorContext _operatorContext;

    public UpdateMyProfileHandler(ISecurityService securityService, IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _operatorContext = operatorContext;
    }

    public async Task<UpdateMyProfileResult> Handle(UpdateMyProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = OperatorActor.RequireUserId(_operatorContext);

        await _securityService.UpdateMyProfileAsync(
            userId,
            request.FirstName ?? "",
            request.LastName ?? "",
            request.CompanyName ?? "",
            cancellationToken);

        return new UpdateMyProfileResult
        {
            Data = "Update MyProfile Success",
        };
    }
}
