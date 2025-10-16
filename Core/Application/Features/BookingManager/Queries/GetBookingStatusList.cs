using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.BookingManager.Queries;

public record GetBookingStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetBookingStatusListProfile : Profile
{
    public GetBookingStatusListProfile()
    {
    }
}

public class GetBookingStatusListResult
{
    public List<GetBookingStatusListDto>? Data { get; init; }
}

public class GetBookingStatusListRequest : IRequest<GetBookingStatusListResult>
{
}


public class GetBookingStatusListHandler : IRequestHandler<GetBookingStatusListRequest, GetBookingStatusListResult>
{

    public GetBookingStatusListHandler()
    {
    }

    public async Task<GetBookingStatusListResult> Handle(GetBookingStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(BookingStatus))
            .Cast<BookingStatus>()
            .Select(status => new GetBookingStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetBookingStatusListResult
        {
            Data = statuses
        };
    }


}



