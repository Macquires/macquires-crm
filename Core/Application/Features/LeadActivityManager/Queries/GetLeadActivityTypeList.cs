using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.DeliveryOrderManager.Queries;

public record GetLeadActivityTypeListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetLeadActivityTypeListProfile : Profile
{
    public GetLeadActivityTypeListProfile()
    {
    }
}

public class GetLeadActivityTypeListResult
{
    public List<GetLeadActivityTypeListDto>? Data { get; init; }
}

public class GetLeadActivityTypeListRequest : IRequest<GetLeadActivityTypeListResult>
{
}


public class GetLeadActivityTypeListHandler : IRequestHandler<GetLeadActivityTypeListRequest, GetLeadActivityTypeListResult>
{

    public GetLeadActivityTypeListHandler()
    {
    }

    public async Task<GetLeadActivityTypeListResult> Handle(GetLeadActivityTypeListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(LeadActivityType))
            .Cast<LeadActivityType>()
            .Select(status => new GetLeadActivityTypeListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetLeadActivityTypeListResult
        {
            Data = statuses
        };
    }


}



