using Application.Common.Integrations;
using Infrastructure.TelecomIntegrations;

namespace Application.Tests.Infrastructure;

public sealed class IntegrationContractTests
{
    [Fact]
    public void BillingPostingIntegration_HasSingleMockImplementation()
    {
        var implementations = typeof(BillingPostingMockIntegration).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IBillingPostingIntegration).IsAssignableFrom(t))
            .Select(t => t.FullName)
            .ToList();

        Assert.Single(implementations);
        Assert.Contains(nameof(BillingPostingMockIntegration), implementations[0], StringComparison.Ordinal);
    }

    [Fact]
    public void BillingPostingIntegration_IsDefinedOnceInApplication()
    {
        var definitions = typeof(IBillingPostingIntegration).Assembly
            .GetTypes()
            .Where(t => t.IsInterface && t.Name == nameof(IBillingPostingIntegration))
            .ToList();

        Assert.Single(definitions);
    }
}
