using Domain.Enums;

namespace Application.Common.Telecom.SellingLine;

public interface IActivationChannelLabelProvider
{
    Task<ActivationChannelLabelsDto> GetAllAsync(CancellationToken cancellationToken = default);

    Task<string> GetLabelArAsync(ActivationChannel channel, CancellationToken cancellationToken = default);
}
