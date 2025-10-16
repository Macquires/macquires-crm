using Application.Common.CQS.Queries;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentMethodManager.Queries;

public record GetPaymentMethodSingleDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}

public class GetPaymentMethodSingleProfile : Profile
{
    public GetPaymentMethodSingleProfile()
    {
        CreateMap<PaymentMethod, GetPaymentMethodSingleDto>();
    }
}

public class GetPaymentMethodSingleResult
{
    public GetPaymentMethodSingleDto? Data { get; init; }
}

public class GetPaymentMethodSingleRequest : IRequest<GetPaymentMethodSingleResult>
{
    public string? Id { get; init; }
}

public class GetPaymentMethodSingleValidator : AbstractValidator<GetPaymentMethodSingleRequest>
{
    public GetPaymentMethodSingleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class GetPaymentMethodSingleHandler : IRequestHandler<GetPaymentMethodSingleRequest, GetPaymentMethodSingleResult>
{
    private readonly IQueryContext _context;
    private readonly IMapper _mapper;

    public GetPaymentMethodSingleHandler(
        IQueryContext context,
        IMapper mapper
        )
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<GetPaymentMethodSingleResult> Handle(GetPaymentMethodSingleRequest request, CancellationToken cancellationToken)
    {
        var query = _context
            .PaymentMethod
            .AsNoTracking()
            .AsQueryable();

        query = query
            .Where(x => x.Id == request.Id);

        var entity = await query.SingleOrDefaultAsync(cancellationToken);

        var dto = _mapper.Map<GetPaymentMethodSingleDto>(entity);

        return new GetPaymentMethodSingleResult
        {
            Data = dto
        };
    }
}