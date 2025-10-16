using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.PurchaseRequisitionManager.Queries;

public record GetPurchaseRequisitionStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetPurchaseRequisitionStatusListProfile : Profile
{
    public GetPurchaseRequisitionStatusListProfile()
    {
    }
}

public class GetPurchaseRequisitionStatusListResult
{
    public List<GetPurchaseRequisitionStatusListDto>? Data { get; init; }
}

public class GetPurchaseRequisitionStatusListRequest : IRequest<GetPurchaseRequisitionStatusListResult>
{
}


public class GetPurchaseRequisitionStatusListHandler : IRequestHandler<GetPurchaseRequisitionStatusListRequest, GetPurchaseRequisitionStatusListResult>
{

    public GetPurchaseRequisitionStatusListHandler()
    {
    }

    public async Task<GetPurchaseRequisitionStatusListResult> Handle(GetPurchaseRequisitionStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(PurchaseRequisitionStatus))
            .Cast<PurchaseRequisitionStatus>()
            .Select(status => new GetPurchaseRequisitionStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetPurchaseRequisitionStatusListResult
        {
            Data = statuses
        };
    }


}



