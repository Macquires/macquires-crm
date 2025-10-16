using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.CreditNoteManager.Queries;

public record GetCreditNoteStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetCreditNoteStatusListProfile : Profile
{
    public GetCreditNoteStatusListProfile()
    {
    }
}

public class GetCreditNoteStatusListResult
{
    public List<GetCreditNoteStatusListDto>? Data { get; init; }
}

public class GetCreditNoteStatusListRequest : IRequest<GetCreditNoteStatusListResult>
{
}

public class GetCreditNoteStatusListHandler : IRequestHandler<GetCreditNoteStatusListRequest, GetCreditNoteStatusListResult>
{
    public GetCreditNoteStatusListHandler()
    {
    }

    public async Task<GetCreditNoteStatusListResult> Handle(GetCreditNoteStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(CreditNoteStatus))
            .Cast<CreditNoteStatus>()
            .Select(status => new GetCreditNoteStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetCreditNoteStatusListResult
        {
            Data = statuses
        };
    }
}