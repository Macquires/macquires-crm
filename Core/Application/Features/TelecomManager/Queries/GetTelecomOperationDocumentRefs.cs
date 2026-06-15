using Application.Common.CQS.Queries;
using Application.Common.Exceptions;
using Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public sealed class GetTelecomOperationDocumentRefsResult
{
    public string? IdentityDocumentStorageKey { get; init; }
    public string? KycDocumentReferenceId { get; init; }
}

public sealed class GetTelecomOperationDocumentRefsRequest : IRequest<GetTelecomOperationDocumentRefsResult>, IRequireAnyPermission
{
    public string Id { get; init; } = null!;
    public IReadOnlyList<string> PermissionKeys => TelecomOperationPermissionSets.OperationsViewAny;
}

public sealed class GetTelecomOperationDocumentRefsHandler
    : IRequestHandler<GetTelecomOperationDocumentRefsRequest, GetTelecomOperationDocumentRefsResult>
{
    private readonly IQueryContext _query;

    public GetTelecomOperationDocumentRefsHandler(IQueryContext query)
    {
        _query = query;
    }

    public async Task<GetTelecomOperationDocumentRefsResult> Handle(
        GetTelecomOperationDocumentRefsRequest request,
        CancellationToken cancellationToken)
    {
        var id = (request.Id ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(id))
        {
            throw new BusinessRuleViolationException("معرّف العملية مطلوب.");
        }

        var refs = await _query.TelecomOperationRequest
            .AsNoTracking()
            .Where(o => !o.IsDeleted && o.Id == id)
            .Select(o => new GetTelecomOperationDocumentRefsResult
            {
                IdentityDocumentStorageKey = o.IdentityDocumentStorageKey,
                KycDocumentReferenceId = o.KycDocumentReferenceId,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return refs ?? new GetTelecomOperationDocumentRefsResult();
    }
}
