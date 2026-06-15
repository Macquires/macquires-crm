using Application.Common.Security;
using Application.Common.Services.SecurityManager;
using FluentValidation;
using MediatR;

namespace Application.Features.SecurityManager.Commands;

public class LogoutResult
{
    public LogoutResultDto? Data { get; set; }
}

public class LogoutRequest : IRequest<LogoutResult>
{
}

public class LogoutValidator : AbstractValidator<LogoutRequest>
{
    public LogoutValidator()
    {
    }
}

public class LogoutHandler : IRequestHandler<LogoutRequest, LogoutResult>
{
    private readonly ISecurityService _securityService;
    private readonly IOperatorContext _operatorContext;

    public LogoutHandler(ISecurityService securityService, IOperatorContext operatorContext)
    {
        _securityService = securityService;
        _operatorContext = operatorContext;
    }

    public async Task<LogoutResult> Handle(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (!_operatorContext.IsAuthenticated || string.IsNullOrWhiteSpace(_operatorContext.UserId))
        {
            return new LogoutResult();
        }

        var result = await _securityService.LogoutAsync(
            _operatorContext.UserId,
            cancellationToken);

        return new LogoutResult { Data = result };
    }
}
