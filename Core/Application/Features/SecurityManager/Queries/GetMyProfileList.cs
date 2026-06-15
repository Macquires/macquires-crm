using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetMyProfileListResult
{
    public List<GetMyProfileListResultDto>? Data { get; init; }
}

public class GetMyProfileListRequest : IRequest<GetMyProfileListResult>, IRequireAuthenticatedOperator
{
}

public class GetMyProfileListHandler : IRequestHandler<GetMyProfileListRequest, GetMyProfileListResult>
{
    private readonly ISecurityService _securityService;
    private readonly IOperatorContext _operatorContext;

    public GetMyProfileListHandler(ISecurityService securityService, IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _operatorContext = operatorContext;
    }

    public async Task<GetMyProfileListResult> Handle(GetMyProfileListRequest request, CancellationToken cancellationToken)
    {
        var userId = OperatorActor.RequireUserId(_operatorContext);

        var result = await _securityService.GetMyProfileListAsync(userId, cancellationToken);

        return new GetMyProfileListResult
        {
            Data = result,
        };
    }
}
