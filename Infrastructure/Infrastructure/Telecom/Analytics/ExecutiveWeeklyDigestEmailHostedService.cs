using Application.Common.Security;
using Application.Common.Services.EmailManager;
using Application.Common.Services.SecurityManager;
using Application.Common.Settings;
using Application.Features.TelecomManager.Queries;
using Infrastructure.Distributed;
using Infrastructure.SecurityManager.Roles;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Telecom.Analytics;

/// <summary>Sends weekly executive digest email to management users (Mondays, UTC).</summary>
public sealed class ExecutiveWeeklyDigestEmailHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);
    private DateOnly? _lastSentWeek;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExecutiveWeeklyDigestEmailHostedService> _logger;

    public ExecutiveWeeklyDigestEmailHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExecutiveWeeklyDigestEmailHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TrySendWeeklyDigestAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Executive weekly digest email tick failed.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task TrySendWeeklyDigestAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (now.DayOfWeek != DayOfWeek.Monday || now.Hour < 6)
        {
            return;
        }

        var week = DateOnly.FromDateTime(now);
        if (_lastSentWeek == week)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        using (scope.ServiceProvider.GetRequiredService<ISystemExecutionGate>().Enter())
        {
            await scope.TryRunUnderDistributedLockAsync(
                "hosted-executive-weekly-digest",
                TimeSpan.FromMinutes(30),
                async ct =>
                {
                    var settings = scope.ServiceProvider.GetRequiredService<IGlobalSettingsProvider>();
                    var enabled = await settings.GetBoolAsync(
                        GlobalSettingKeys.ExecutiveDigestEmailEnabled,
                        false,
                        ct);
                    if (!enabled)
                    {
                        return;
                    }

                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    var digest = await mediator.Send(new GetExecutiveWeeklyDigestRequest(null, null), ct);
                    var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
                    var security = scope.ServiceProvider.GetRequiredService<ISecurityService>();

                    var managementRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        TelecomRoles.Management,
                        TelecomRoles.Admin,
                    };

                    var recipients = (await security.GetUserListAsync(null, ct))
                        .Where(u => u is { IsDeleted: false or null, IsBlocked: false or null, Email: not null }
                            && (u.Roles?.Any(r => managementRoles.Contains(r)) == true))
                        .Select(u => u.Email!)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (recipients.Count == 0)
                    {
                        return;
                    }

                    var insights = digest.AiInsightsAr.Count > 0
                        ? string.Join("<br/>", digest.AiInsightsAr.Select(i => $"• {i}"))
                        : digest.SummaryAr;

                    var html = $"""
                        <div dir="rtl" style="font-family:Tahoma,sans-serif">
                        <h2>ملخص الأسبوع التنفيذي — سيريتل CRM</h2>
                        <p><strong>النطاق:</strong> {digest.ScopeLabelAr}</p>
                        <p>{digest.SummaryAr}</p>
                        <h3>رؤى تحليلية</h3>
                        <p>{insights}</p>
                        <p style="color:#666;font-size:12px">أُرسل تلقائياً من nSuite — {now:yyyy-MM-dd HH:mm} UTC</p>
                        </div>
                        """;

                    foreach (var recipient in recipients)
                    {
                        await email.SendEmailAsync(recipient, "ملخص الأسبوع التنفيذي — سيريتل CRM", html);
                    }

                    var teamsWebhook = await settings.GetValueAsync(
                        GlobalSettingKeys.ExecutiveDigestTeamsWebhookUrl,
                        ct);
                    if (!string.IsNullOrWhiteSpace(teamsWebhook))
                    {
                        await PostTeamsWebhookAsync(teamsWebhook, digest.SummaryAr, ct);
                    }

                    _lastSentWeek = week;
                    _logger.LogInformation("Executive weekly digest emailed to {Count} recipients.", recipients.Count);
                },
                cancellationToken);
        }
    }

    private static async Task PostTeamsWebhookAsync(string webhookUrl, string summary, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        var payload = new { text = $"**ملخص الأسبوع التنفيذي**\n\n{summary}" };
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        await client.PostAsync(webhookUrl, content, cancellationToken);
    }
}
