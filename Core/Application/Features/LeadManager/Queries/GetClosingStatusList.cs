using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.CampaignManager.Queries;

public record GetClosingStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetClosingStatusListProfile : Profile
{
    public GetClosingStatusListProfile()
    {
    }
}

public class GetClosingStatusListResult
{
    public List<GetClosingStatusListDto>? Data { get; init; }
}

public class GetClosingStatusListRequest : IRequest<GetClosingStatusListResult>
{
}

public class GetClosingStatusListHandler : IRequestHandler<GetClosingStatusListRequest, GetClosingStatusListResult>
{
    public GetClosingStatusListHandler()
    {
    }

    public async Task<GetClosingStatusListResult> Handle(GetClosingStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(ClosingStatus))
            .Cast<ClosingStatus>()
            .Select(status => new GetClosingStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetClosingStatusListResult
        {
            Data = statuses
        };
    }
}