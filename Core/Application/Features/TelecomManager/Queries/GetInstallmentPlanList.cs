using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.TelecomManager.Queries;

public record GetInstallmentPlanListDto(
    string Id,
    string Code,
    string NameAr,
    int Months,
    decimal MinDownPaymentPercent,
    decimal InterestRatePercent,
    int MinCreditScore);

public class GetInstallmentPlanListResult
{
    public List<GetInstallmentPlanListDto>? Data { get; init; }
}

public class GetInstallmentPlanListRequest : IRequest<GetInstallmentPlanListResult>;

public class GetInstallmentPlanListHandler : IRequestHandler<GetInstallmentPlanListRequest, GetInstallmentPlanListResult>
{
    private readonly IQueryContext _context;

    public GetInstallmentPlanListHandler(IQueryContext context) => _context = context;

    public async Task<GetInstallmentPlanListResult> Handle(
        GetInstallmentPlanListRequest request,
        CancellationToken cancellationToken)
    {
        var data = await _context.InstallmentPlan.AsNoTracking().IsDeletedEqualTo()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Months)
            .Select(p => new GetInstallmentPlanListDto(
                p.Id, p.Code, p.NameAr, p.Months, p.MinDownPaymentPercent, p.InterestRatePercent, p.MinCreditScore))
            .ToListAsync(cancellationToken);

        return new GetInstallmentPlanListResult { Data = data };
    }
}
