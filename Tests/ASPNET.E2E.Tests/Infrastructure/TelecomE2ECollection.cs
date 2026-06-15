using Xunit;

namespace ASPNET.E2E.Tests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class TelecomE2ECollection : ICollectionFixture<TelecomE2EFixture>
{
    public const string Name = "TelecomE2E";
}

[CollectionDefinition(ActivationE2ECollection.Name)]
public sealed class ActivationE2ECollection : ICollectionFixture<TelecomE2EFixture>
{
    public const string Name = "ActivationE2E";
}
