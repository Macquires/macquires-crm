using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.ProgramManagerManager.Queries;

public record GetProgramManagerStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetProgramManagerStatusListProfile : Profile
{
    public GetProgramManagerStatusListProfile()
    {
    }
}

public class GetProgramManagerStatusListResult
{
    public List<GetProgramManagerStatusListDto>? Data { get; init; }
}

public class GetProgramManagerStatusListRequest : IRequest<GetProgramManagerStatusListResult>
{
}


public class GetProgramManagerStatusListHandler : IRequestHandler<GetProgramManagerStatusListRequest, GetProgramManagerStatusListResult>
{

    public GetProgramManagerStatusListHandler()
    {
    }

    public async Task<GetProgramManagerStatusListResult> Handle(GetProgramManagerStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(ProgramManagerStatus))
            .Cast<ProgramManagerStatus>()
            .Select(status => new GetProgramManagerStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetProgramManagerStatusListResult
        {
            Data = statuses
        };
    }


}



