using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.ExpenseManager.Queries;

public record GetExpenseStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetExpenseStatusListProfile : Profile
{
    public GetExpenseStatusListProfile()
    {
    }
}

public class GetExpenseStatusListResult
{
    public List<GetExpenseStatusListDto>? Data { get; init; }
}

public class GetExpenseStatusListRequest : IRequest<GetExpenseStatusListResult>
{
}

public class GetExpenseStatusListHandler : IRequestHandler<GetExpenseStatusListRequest, GetExpenseStatusListResult>
{
    public GetExpenseStatusListHandler()
    {
    }

    public async Task<GetExpenseStatusListResult> Handle(GetExpenseStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(ExpenseStatus))
            .Cast<ExpenseStatus>()
            .Select(status => new GetExpenseStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetExpenseStatusListResult
        {
            Data = statuses
        };
    }
}