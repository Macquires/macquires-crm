using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.SalesQuotationManager.Queries;

public record GetSalesQuotationStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetSalesQuotationStatusListProfile : Profile
{
    public GetSalesQuotationStatusListProfile()
    {
    }
}

public class GetSalesQuotationStatusListResult
{
    public List<GetSalesQuotationStatusListDto>? Data { get; init; }
}

public class GetSalesQuotationStatusListRequest : IRequest<GetSalesQuotationStatusListResult>
{
}


public class GetSalesQuotationStatusListHandler : IRequestHandler<GetSalesQuotationStatusListRequest, GetSalesQuotationStatusListResult>
{

    public GetSalesQuotationStatusListHandler()
    {
    }

    public async Task<GetSalesQuotationStatusListResult> Handle(GetSalesQuotationStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(SalesQuotationStatus))
            .Cast<SalesQuotationStatus>()
            .Select(status => new GetSalesQuotationStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetSalesQuotationStatusListResult
        {
            Data = statuses
        };
    }


}



