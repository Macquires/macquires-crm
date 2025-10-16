using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.CampaignManager.Queries;

public record GetCampaignStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetCampaignStatusListProfile : Profile
{
    public GetCampaignStatusListProfile()
    {
    }
}

public class GetCampaignStatusListResult
{
    public List<GetCampaignStatusListDto>? Data { get; init; }
}

public class GetCampaignStatusListRequest : IRequest<GetCampaignStatusListResult>
{
}

public class GetCampaignStatusListHandler : IRequestHandler<GetCampaignStatusListRequest, GetCampaignStatusListResult>
{
    public GetCampaignStatusListHandler()
    {
    }

    public async Task<GetCampaignStatusListResult> Handle(GetCampaignStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(CampaignStatus))
            .Cast<CampaignStatus>()
            .Select(status => new GetCampaignStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetCampaignStatusListResult
        {
            Data = statuses
        };
    }
}