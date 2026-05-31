using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CustomerManager.Queries;

public class RevealCustomerNationalIdResult
{
    public string? NationalId { get; init; }
    public bool Allowed { get; init; }
}

public class RevealCustomerNationalIdRequest : IRequest<RevealCustomerNationalIdResult>
{
    public string CustomerId { get; init; } = "";
}

public class RevealCustomerNationalIdValidator : AbstractValidator<RevealCustomerNationalIdRequest>
{
    public RevealCustomerNationalIdValidator() => RuleFor(x => x.CustomerId).NotEmpty();
}

public class RevealCustomerNationalIdHandler : IRequestHandler<RevealCustomerNationalIdRequest, RevealCustomerNationalIdResult>
{
    private readonly IQueryContext _query;

    public RevealCustomerNationalIdHandler(IQueryContext query) => _query = query;

    public async Task<RevealCustomerNationalIdResult> Handle(
        RevealCustomerNationalIdRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await _query.Customer.AsNoTracking().IsDeletedEqualTo()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer is not IndividualCustomer ind)
        {
            return new RevealCustomerNationalIdResult { Allowed = false };
        }

        return new RevealCustomerNationalIdResult
        {
            Allowed = true,
            NationalId = ind.NationalId
        };
    }
}
