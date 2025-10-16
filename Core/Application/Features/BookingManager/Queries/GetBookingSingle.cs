using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BookingManager.Queries;

public class GetBookingSingleProfile : Profile
{
    public GetBookingSingleProfile()
    {
    }
}

public class GetBookingSingleResult
{
    public Booking? Data { get; init; }
}

public class GetBookingSingleRequest : IRequest<GetBookingSingleResult>
{
    public string? Id { get; init; }
}

public class GetBookingSingleValidator : AbstractValidator<GetBookingSingleRequest>
{
    public GetBookingSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetBookingSingleHandler : IRequestHandler<GetBookingSingleRequest, GetBookingSingleResult>
{
    private readonly IQueryContext _context;
    private readonly IMapper _mapper;

    public GetBookingSingleHandler(
        IQueryContext context,
        IMapper mapper
        )
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetBookingSingleResult> Handle(GetBookingSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .Booking
            .AsNoTracking()
            .Include(x => x.BookingResource)
                .ThenInclude(x => x!.BookingGroup)
            .Where(x => x.Id == request.Id)
            .AsQueryable();

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        return new GetBookingSingleResult
        {
            Data = entity
        };
    }
}