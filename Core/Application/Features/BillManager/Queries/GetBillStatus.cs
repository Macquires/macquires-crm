using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.BillManager.Queries;

public record GetBillStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetBillStatusListProfile : Profile
{
    public GetBillStatusListProfile()
    {
    }
}

public class GetBillStatusListResult
{
    public List<GetBillStatusListDto>? Data { get; init; }
}

public class GetBillStatusListRequest : IRequest<GetBillStatusListResult>
{
}

public class GetBillStatusListHandler : IRequestHandler<GetBillStatusListRequest, GetBillStatusListResult>
{
    public GetBillStatusListHandler()
    {
    }

    public async Task<GetBillStatusListResult> Handle(GetBillStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(BillStatus))
            .Cast<BillStatus>()
            .Select(status => new GetBillStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetBillStatusListResult
        {
            Data = statuses
        };
    }
}