using Application.Common.Extensions;
using AutoMapper;
using Domain.Enums;
using MediatR;

namespace Application.Features.DebitNoteManager.Queries;

public record GetDebitNoteStatusListDto
{
    public string? Id { get; init; }
    public string? Name { get; init; }
}

public class GetDebitNoteStatusListProfile : Profile
{
    public GetDebitNoteStatusListProfile()
    {
    }
}

public class GetDebitNoteStatusListResult
{
    public List<GetDebitNoteStatusListDto>? Data { get; init; }
}

public class GetDebitNoteStatusListRequest : IRequest<GetDebitNoteStatusListResult>
{
}

public class GetDebitNoteStatusListHandler : IRequestHandler<GetDebitNoteStatusListRequest, GetDebitNoteStatusListResult>
{
    public GetDebitNoteStatusListHandler()
    {
    }

    public async Task<GetDebitNoteStatusListResult> Handle(GetDebitNoteStatusListRequest request, CancellationToken cancellationToken)
    {
        var statuses = Enum.GetValues(typeof(DebitNoteStatus))
            .Cast<DebitNoteStatus>()
            .Select(status => new GetDebitNoteStatusListDto
            {
                Id = ((int)status).ToString(),
                Name = status.ToFriendlyName()
            })
            .ToList();

        await Task.CompletedTask;

        return new GetDebitNoteStatusListResult
        {
            Data = statuses
        };
    }
}