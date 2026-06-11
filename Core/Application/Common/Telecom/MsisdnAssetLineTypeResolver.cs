using Domain.Entities;

namespace Application.Common.Telecom;

public static class MsisdnAssetLineTypeResolver
{
    public static string? ResolveTypeId(MsisdnAsset asset) =>
        asset.IntendedSubscriptionTypeId ?? asset.Product?.CompatibleSubscriptionTypeId;

    public static TelecomSubscriptionTypeLookup? ResolveLookup(MsisdnAsset asset) =>
        asset.IntendedSubscriptionTypeLookup ?? asset.Product?.CompatibleSubscriptionTypeLookup;
}
