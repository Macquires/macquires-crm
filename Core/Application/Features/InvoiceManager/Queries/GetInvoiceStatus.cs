using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.InvoiceManager.Queries;

public record GetInvoiceStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetInvoiceStatusListProfile : Profile
{
    public GetInvoiceStatusListProfile()
    {
    }
}

public class GetInvoiceStatusListResult
{
    public List<GetInvoiceStatusListDto>? Data { get; init; }
}

public class GetInvoiceStatusListRequest : IRequest<GetInvoiceStatusListResult>
{
}

public class GetInvoiceStatusListHandler : IRequestHandler<GetInvoiceStatusListRequest, GetInvoiceStatusListResult>
{
    public GetInvoiceStatusListHandler()
    {
    }

    public async Task<GetInvoiceStatusListResult> Handle(GetInvoiceStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(InvoiceStatus))
            .Cast<InvoiceStatus>()
            .Select(status => new GetInvoiceStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetInvoiceStatusListResult
        {
            Data = statuses
        };
    }
}