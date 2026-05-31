using Application.Common.Settings;
using MediatR;

namespace Application.Features.SecurityManager.Queries;

public class GetGlobalSettingsResult
{
    public GlobalSettingsSnapshotDto? Data { get; init; }
}

public class GetGlobalSettingsRequest : IRequest<GetGlobalSettingsResult>
{
}

public class GetGlobalSettingsHandler : IRequestHandler<GetGlobalSettingsRequest, GetGlobalSettingsResult>
{
    private readonly IGlobalSettingsAdminService _admin;

    public GetGlobalSettingsHandler(IGlobalSettingsAdminService admin) => _admin = admin;

    public async Task<GetGlobalSettingsResult> Handle(GetGlobalSettingsRequest request, CancellationToken cancellationToken)
    {
        var data = await _admin.GetSnapshotAsync(cancellationToken);
        return new GetGlobalSettingsResult { Data = data };
    }
}
