using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.PaymentDisburseManager.Queries;

public record GetPaymentDisburseStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetPaymentDisburseStatusListProfile : Profile
{
    public GetPaymentDisburseStatusListProfile()
    {
    }
}

public class GetPaymentDisburseStatusListResult
{
    public List<GetPaymentDisburseStatusListDto>? Data { get; init; }
}

public class GetPaymentDisburseStatusListRequest : IRequest<GetPaymentDisburseStatusListResult>
{
}

public class GetPaymentDisburseStatusListHandler : IRequestHandler<GetPaymentDisburseStatusListRequest, GetPaymentDisburseStatusListResult>
{
    public GetPaymentDisburseStatusListHandler()
    {
    }

    public async Task<GetPaymentDisburseStatusListResult> Handle(GetPaymentDisburseStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(PaymentDisburseStatus))
            .Cast<PaymentDisburseStatus>()
            .Select(status => new GetPaymentDisburseStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetPaymentDisburseStatusListResult
        {
            Data = statuses
        };
    }
}