using Application.Common.Security;
using Application.Common.Telecom.Analytics;
using MediatR;

namespace Application.Features.TelecomManager.Queries;

public sealed class ExecutiveWeeklyDigestHighlightDto
{
    public string LabelAr { get; init; } = null!;
    public string ValueAr { get; init; } = null!;
    public string? TrendAr { get; init; }
}

public class GetExecutiveWeeklyDigestResult
{
    public string ScopeLabelAr { get; init; } = null!;
    public DateTime WeekEndingUtc { get; init; }
    public string SummaryAr { get; init; } = null!;
    public IReadOnlyList<string> AiInsightsAr { get; init; } = [];
    public IReadOnlyList<ExecutiveWeeklyDigestHighlightDto> Highlights { get; init; } = [];
    public IReadOnlyList<ExecutiveExceptionItemDto> TopExceptions { get; init; } = [];
}

public record GetExecutiveWeeklyDigestRequest(string? RegionId, string? BranchId)
    : IRequest<GetExecutiveWeeklyDigestResult>, IOperationalKpiRequest;

public class GetExecutiveWeeklyDigestHandler
    : IRequestHandler<GetExecutiveWeeklyDigestRequest, GetExecutiveWeeklyDigestResult>
{
    private const int WeeklyCycleDays = 7;

    private readonly IOperationalAnalyticsScopeService _scopeService;
    private readonly IExecutiveFinancialMetricsService _financialMetrics;
    private readonly IMediator _mediator;

    public GetExecutiveWeeklyDigestHandler(
        IOperationalAnalyticsScopeService scopeService,
        IExecutiveFinancialMetricsService financialMetrics,
        IMediator mediator)
    {
        _scopeService = scopeService;
        _financialMetrics = financialMetrics;
        _mediator = mediator;
    }

    public async Task<GetExecutiveWeeklyDigestResult> Handle(
        GetExecutiveWeeklyDigestRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await _scopeService.ResolveScopeAsync(
            request.RegionId,
            request.BranchId,
            cancellationToken);

        var weekEnding = DateTime.UtcNow;
        var metrics = await _financialMetrics.ComputeAsync(
            scope.EffectiveBranchIds,
            weekEnding,
            WeeklyCycleDays,
            cancellationToken);

        var exceptions = await _mediator.Send(
            new GetExecutiveExceptionsRequest(request.RegionId, request.BranchId),
            cancellationToken);

        var topExceptions = exceptions.Items
            .OrderByDescending(i => i.Severity)
            .ThenByDescending(i => i.OccurredAtUtc ?? DateTime.MinValue)
            .Take(5)
            .ToList();

        var highlights = BuildHighlights(metrics, exceptions);
        var aiInsights = BuildAiInsights(metrics, exceptions, topExceptions);
        var summary = BuildSummaryAr(metrics, exceptions, topExceptions);

        return new GetExecutiveWeeklyDigestResult
        {
            ScopeLabelAr = scope.ScopeLabelAr,
            WeekEndingUtc = weekEnding,
            SummaryAr = summary,
            AiInsightsAr = aiInsights,
            Highlights = highlights,
            TopExceptions = topExceptions,
        };
    }

    private static List<ExecutiveWeeklyDigestHighlightDto> BuildHighlights(
        ExecutiveFinancialMetrics metrics,
        GetExecutiveExceptionsResult exceptions)
    {
        var revenueTrend = metrics.RevenueChangePercent >= 0
            ? $"+{metrics.RevenueChangePercent:N1}%"
            : $"{metrics.RevenueChangePercent:N1}%";

        return
        [
            new ExecutiveWeeklyDigestHighlightDto
            {
                LabelAr = "إيراد الأسبوع",
                ValueAr = $"{metrics.TotalRevenue:N0} ل.س",
                TrendAr = $"مقارنة بالأسبوع السابق {revenueTrend}",
            },
            new ExecutiveWeeklyDigestHighlightDto
            {
                LabelAr = "ARPU",
                ValueAr = $"{metrics.Arpu:N0} ل.س",
            },
            new ExecutiveWeeklyDigestHighlightDto
            {
                LabelAr = "Churn (30 يوم)",
                ValueAr = $"{metrics.ChurnPercent30:N1}%",
            },
            new ExecutiveWeeklyDigestHighlightDto
            {
                LabelAr = "تنبيهات حرجة",
                ValueAr = exceptions.CriticalCount.ToString(),
                TrendAr = exceptions.WarningCount > 0
                    ? $"{exceptions.WarningCount} تحذير"
                    : null,
            },
        ];
    }

    private static string BuildSummaryAr(
        ExecutiveFinancialMetrics metrics,
        GetExecutiveExceptionsResult exceptions,
        IReadOnlyList<ExecutiveExceptionItemDto> topExceptions)
    {
        if (metrics.TotalRevenue == 0 && exceptions.TotalCount == 0)
        {
            return "لا توجد بيانات تشغيلية كافية في نطاق الصلاحية الحالي لهذا الأسبوع.";
        }

        var revenuePart = metrics.RevenueChangePercent >= 0
            ? $"ارتفعت الإيرادات {metrics.RevenueChangePercent:N1}% عن الأسبوع السابق"
            : $"انخفضت الإيرادات {Math.Abs(metrics.RevenueChangePercent):N1}% عن الأسبوع السابق";

        var churnPart = metrics.ChurnPercent30 > 5m
            ? $"مع معدل فقد مشتركين مرتفع ({metrics.ChurnPercent30:N1}%)."
            : $"ومعدل فقد مشتركين مستقر ({metrics.ChurnPercent30:N1}%).";

        if (exceptions.CriticalCount == 0 && exceptions.WarningCount == 0)
        {
            return $"خلال الأسبوع: {revenuePart} {churnPart} لا توجد استثناءات تشغيلية تتطلب تدخلًا فوريًا.";
        }

        var lead = topExceptions.FirstOrDefault()?.TitleAr ?? "استثناءات تشغيلية";
        return $"خلال الأسبوع: {revenuePart} {churnPart} يوجد {exceptions.CriticalCount} تنبيه حرج و{exceptions.WarningCount} تحذير — أبرزها: {lead}.";
    }

    private static List<string> BuildAiInsights(
        ExecutiveFinancialMetrics metrics,
        GetExecutiveExceptionsResult exceptions,
        IReadOnlyList<ExecutiveExceptionItemDto> topExceptions)
    {
        var insights = new List<string>();

        if (metrics.RevenueChangePercent <= -10m)
        {
            insights.Add($"⚠️ ضغط إيرادي: تراجع {Math.Abs(metrics.RevenueChangePercent):N1}% يستدعي مراجعة عروض الفروع الأضعف.");
        }
        else if (metrics.RevenueChangePercent >= 10m)
        {
            insights.Add($"📈 زخم إيجابي: نمو إيراد {metrics.RevenueChangePercent:N1}% — فرصة لتوسيع الحملات في الفروع الرابحة.");
        }

        if (metrics.ChurnPercent30 > 5m)
        {
            insights.Add($"🔻 Churn مرتفع ({metrics.ChurnPercent30:N1}%) — ركّز على استرجاع المشتركين المعلّقين قبل الإنهاء.");
        }

        var topBranch = metrics.BranchHeat.OrderByDescending(b => b.Revenue).FirstOrDefault();
        if (topBranch != null && metrics.TotalRevenue > 0)
        {
            var share = topBranch.Revenue / metrics.TotalRevenue * 100m;
            insights.Add($"🏢 {topBranch.BranchName} يمثّل {share:N0}% من الإيراد — راقب تركّز المخاطر التشغيلية.");
        }

        if (exceptions.CriticalCount > 0)
        {
            var codes = topExceptions.Select(e => e.TitleAr).Take(2);
            insights.Add($"🚨 أولوية اليوم: {string.Join("، ", codes)}.");
        }
        else if (exceptions.WarningCount > 0)
        {
            insights.Add($"⏳ {exceptions.WarningCount} تحذير تشغيلي — معالجة استباقية قبل تصعيدها.");
        }
        else
        {
            insights.Add("✅ لا استثناءات حرجة — استمر بمراقبة SLA والتكامل.");
        }

        return insights;
    }
}
