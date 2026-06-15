using Application.Common.Security;
using Application.Common.Telecom.SellingLine;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public class GetActivationChannelLabelsResult
{
    public ActivationChannelLabelsDto? Data { get; init; }
}

public class GetActivationChannelLabelsRequest : IRequest<GetActivationChannelLabelsResult>, IRequireAnyPermission
{
    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.OperationsViewAny;
}

public class GetActivationChannelLabelsHandler : IRequestHandler<GetActivationChannelLabelsRequest, GetActivationChannelLabelsResult>
{
    private readonly IActivationChannelLabelProvider _labels;

    public GetActivationChannelLabelsHandler(IActivationChannelLabelProvider labels) => _labels = labels;

    public async Task<GetActivationChannelLabelsResult> Handle(
        GetActivationChannelLabelsRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _labels.GetAllAsync(cancellationToken);
        return new GetActivationChannelLabelsResult { Data = data };
    }
}
