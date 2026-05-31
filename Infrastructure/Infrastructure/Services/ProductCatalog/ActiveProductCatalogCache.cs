using Application.Common.Services.ProductCatalog;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services.ProductCatalog;

public sealed class ActiveProductCatalogCache : IActiveProductCatalogCache
{
    public const string ActiveOfferingsKey = "catalog:active-offerings";

    private readonly IMemoryCache _cache;

    public ActiveProductCatalogCache(IMemoryCache cache) => _cache = cache;

    public void Invalidate() => _cache.Remove(ActiveOfferingsKey);
}
