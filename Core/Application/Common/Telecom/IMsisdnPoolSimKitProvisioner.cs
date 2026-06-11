using Domain.Entities;

namespace Application.Common.Telecom;

/// <summary>
/// Ensures a physical SIM kit exists in <see cref="SimInventory"/> for MSISDN pool assets.
/// </summary>
public interface IMsisdnPoolSimKitProvisioner
{
    Task EnsureForPoolAssetAsync(MsisdnAsset asset, bool reserveSim, CancellationToken cancellationToken = default);
}
