using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.BudgetManager.Queries;

public record GetBudgetStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetBudgetStatusListProfile : Profile
{
    public GetBudgetStatusListProfile()
    {
    }
}

public class GetBudgetStatusListResult
{
    public List<GetBudgetStatusListDto>? Data { get; init; }
}

public class GetBudgetStatusListRequest : IRequest<GetBudgetStatusListResult>
{
}

public class GetBudgetStatusListHandler : IRequestHandler<GetBudgetStatusListRequest, GetBudgetStatusListResult>
{

    public GetBudgetStatusListHandler()
    {
    }

    public async Task<GetBudgetStatusListResult> Handle(GetBudgetStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(BudgetStatus))
            .Cast<BudgetStatus>()
            .Select(status => new GetBudgetStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetBudgetStatusListResult
        {
            Data = statuses
        };
    }
}