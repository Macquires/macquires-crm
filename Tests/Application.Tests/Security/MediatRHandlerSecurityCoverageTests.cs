using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using Application.Features.SecurityManager.Commands;
using Application.Features.SecurityManager.Queries;
using MediatR;

namespace Application.Tests.Security;

public sealed class MediatRHandlerSecurityCoverageTests
{
    private static readonly HashSet<string> IntentionallyPublicHandlers =
    [
        nameof(LoginHandler),
        nameof(LogoutHandler),
        nameof(RegisterHandler),
        nameof(RefreshTokenHandler),
        nameof(ConfirmEmailHandler),
        nameof(ForgotPasswordHandler),
        nameof(ForgotPasswordConfirmationHandler),
    ];

    [Fact]
    public void FeatureRequests_ImplementSecurityMarker_OrArePublicAuth()
    {
        var requestTypes = typeof(LoginRequest).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.GetInterfaces().Any(i =>
                            i.IsGenericType
                            && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .Where(t => t.Namespace?.StartsWith("Application.Features", StringComparison.Ordinal) == true)
            .ToList();

        var missing = new List<string>();
        foreach (var type in requestTypes)
        {
            if (HasSecurityMarker(type))
            {
                continue;
            }

            var handlerName = type.Name.Replace("Request", "Handler", StringComparison.Ordinal);
            if (IntentionallyPublicHandlers.Contains(handlerName))
            {
                continue;
            }

            missing.Add(type.FullName ?? type.Name);
        }

        Assert.True(
            missing.Count == 0,
            $"Unsecured MediatR requests:{Environment.NewLine}{string.Join(Environment.NewLine, missing.OrderBy(x => x))}");
    }

    private static bool HasSecurityMarker(Type type) =>
        typeof(IRequirePermission).IsAssignableFrom(type)
        || typeof(IRequireAnyPermission).IsAssignableFrom(type)
        || typeof(IRequireAuthenticatedOperator).IsAssignableFrom(type)
        || typeof(IOperationalKpiRequest).IsAssignableFrom(type);
}
