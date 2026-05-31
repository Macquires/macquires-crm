using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetTelecomOperationDetailDto
{
    public string? Id { get; init; }
    public string? Number { get; init; }
    public TelecomOperationKind Kind { get; init; }
    public string? KindLabelAr { get; init; }
    public TelecomOperationStatus Status { get; init; }
    public string? StatusLabelAr { get; init; }
    public TelecomDocumentStatus DocumentStatus { get; init; }
    public string? Msisdn { get; init; }
    public string? CurrentOwnerName { get; init; }
    public string? NewOwnerName { get; init; }
    public string? Notes { get; init; }
    public bool HasIdentityDocument { get; init; }
    public DateTime? CreatedAtUtc { get; init; }
}

public class GetTelecomOperationDetailResult
{
    public GetTelecomOperationDetailDto? Data { get; init; }
}

public class GetTelecomOperationDetailRequest : IRequest<GetTelecomOperationDetailResult>
{
    public string Id { get; init; } = null!;
}

public class GetTelecomOperationDetailHandler : IRequestHandler<GetTelecomOperationDetailRequest, GetTelecomOperationDetailResult>
{
    private readonly IQueryContext _context;

    public GetTelecomOperationDetailHandler(IQueryContext context) => _context = context;

    public async Task<GetTelecomOperationDetailResult> Handle(
        GetTelecomOperationDetailRequest request,
        CancellationToken cancellationToken)
    {
        var op = await _context.TelecomOperationRequest
            .AsNoTracking()
            .IsDeletedEqualTo(false)
            .Include(x => x.MsisdnAsset)
            .Include(x => x.SubscriberProfile!)
                .ThenInclude(p => p!.Customer)
            .Include(x => x.SecondarySubscriberProfile!)
                .ThenInclude(p => p!.Customer)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (op == null)
        {
            return new GetTelecomOperationDetailResult();
        }

        return new GetTelecomOperationDetailResult
        {
            Data = new GetTelecomOperationDetailDto
            {
                Id = op.Id,
                Number = op.Number,
                Kind = op.Kind,
                KindLabelAr = KindLabelAr(op.Kind),
                Status = op.Status,
                StatusLabelAr = StatusLabelAr(op.Status),
                DocumentStatus = op.DocumentStatus,
                Msisdn = op.MsisdnAsset?.Msisdn,
                CurrentOwnerName = op.SubscriberProfile?.Customer?.DisplayName,
                NewOwnerName = op.SecondarySubscriberProfile?.Customer?.DisplayName,
                Notes = op.Notes,
                HasIdentityDocument = !string.IsNullOrWhiteSpace(op.IdentityDocumentStorageKey),
                CreatedAtUtc = op.CreatedAtUtc
            }
        };
    }

    private static string KindLabelAr(TelecomOperationKind kind) => kind switch
    {
        TelecomOperationKind.TakeOver => "نقل ملكية",
        TelecomOperationKind.Migration => "تحويل باقة",
        TelecomOperationKind.NewActivation => "تفعيل جديد",
        TelecomOperationKind.SimSwap => "تبديل شريحة",
        _ => kind.ToString()
    };

    private static string StatusLabelAr(TelecomOperationStatus status) => status switch
    {
        TelecomOperationStatus.Draft => "مسودة",
        TelecomOperationStatus.PendingDocuments => "قيد التدقيق القانوني",
        TelecomOperationStatus.Confirmed => "مؤكد محلياً",
        TelecomOperationStatus.Provisioning => "تجهيز الشبكة",
        TelecomOperationStatus.Completed => "منجز",
        TelecomOperationStatus.Failed => "فشل",
        TelecomOperationStatus.PendingExternal => "مزامنة خارجية",
        _ => status.ToString()
    };
}
