using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.CampaignManager.Queries;

public record GetPipelineStageListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetPipelineStageListProfile : Profile
{
    public GetPipelineStageListProfile()
    {
    }
}

public class GetPipelineStageListResult
{
    public List<GetPipelineStageListDto>? Data { get; init; }
}

public class GetPipelineStageListRequest : IRequest<GetPipelineStageListResult>
{
}

public class GetPipelineStageListHandler : IRequestHandler<GetPipelineStageListRequest, GetPipelineStageListResult>
{
    public GetPipelineStageListHandler()
    {
    }

    public async Task<GetPipelineStageListResult> Handle(GetPipelineStageListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(PipelineStage))
            .Cast<PipelineStage>()
            .Select(status => new GetPipelineStageListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetPipelineStageListResult
        {
            Data = statuses
        };
    }
}