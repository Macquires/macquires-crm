using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.ProgramManagerManager.Queries;

public record GetProgramManagerPriorityListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetProgramManagerPriorityListProfile : Profile
{
    public GetProgramManagerPriorityListProfile()
    {
    }
}

public class GetProgramManagerPriorityListResult
{
    public List<GetProgramManagerPriorityListDto>? Data { get; init; }
}

public class GetProgramManagerPriorityListRequest : IRequest<GetProgramManagerPriorityListResult>
{
}


public class GetProgramManagerPriorityListHandler : IRequestHandler<GetProgramManagerPriorityListRequest, GetProgramManagerPriorityListResult>
{

    public GetProgramManagerPriorityListHandler()
    {
    }

    public async Task<GetProgramManagerPriorityListResult> Handle(GetProgramManagerPriorityListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(ProgramManagerPriority))
            .Cast<ProgramManagerPriority>()
            .Select(status => new GetProgramManagerPriorityListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetProgramManagerPriorityListResult
        {
            Data = statuses
        };
    }


}



