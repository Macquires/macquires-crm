using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.PaymentReceiveManager.Queries;

public record GetPaymentReceiveStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetPaymentReceiveStatusListProfile : Profile
{
    public GetPaymentReceiveStatusListProfile()
    {
    }
}

public class GetPaymentReceiveStatusListResult
{
    public List<GetPaymentReceiveStatusListDto>? Data { get; init; }
}

public class GetPaymentReceiveStatusListRequest : IRequest<GetPaymentReceiveStatusListResult>
{
}

public class GetPaymentReceiveStatusListHandler : IRequestHandler<GetPaymentReceiveStatusListRequest, GetPaymentReceiveStatusListResult>
{
    public GetPaymentReceiveStatusListHandler()
    {
    }

    public async Task<GetPaymentReceiveStatusListResult> Handle(GetPaymentReceiveStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(PaymentReceiveStatus))
            .Cast<PaymentReceiveStatus>()
            .Select(status => new GetPaymentReceiveStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetPaymentReceiveStatusListResult
        {
            Data = statuses
        };
    }
}