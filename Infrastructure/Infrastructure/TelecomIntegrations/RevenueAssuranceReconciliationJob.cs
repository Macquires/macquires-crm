using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Security;
using Application.Common.Telecom;
using Application.Common.Telecom.RevenueAssurance;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Distributed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.TelecomIntegrations;

/// <summary>
/// §1 Revenue Assurance Reconciliation Job.
/// Scans subscriptions and compares financial state (CRM) against technical state (HLR).
/// If CRM is Suspended but HLR is ACTIVE, it identifies a Technical Desync (Revenue Leakage).
/// </summary>
public sealed class RevenueAssuranceReconciliationJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RevenueAssuranceReconciliationJob> _logger;

    public RevenueAssuranceReconciliationJob(
        IServiceProvider serviceProvider,
        ILogger<RevenueAssuranceReconciliationJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RevenueAssuranceReconciliationJob starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                using (scope.ServiceProvider.GetRequiredService<ISystemExecutionGate>().Enter())
                {
                    await scope.TryRunUnderDistributedLockAsync(
                        "hosted-revenue-assurance-reconciliation",
                        TimeSpan.FromHours(2),
                        async ct =>
                        {
                            var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
                            var scanner = scope.ServiceProvider.GetRequiredService<IRevenueAssuranceLeakageScanner>();
                            var ticketRepo = scope.ServiceProvider.GetRequiredService<ICommandRepository<TelecomTechnicalTicket>>();
                            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                            var suspendedSubs = await query.TelecomSubscription
                                .Include(s => s.MsisdnAsset)
                                .Include(s => s.SubscriberProfile)
                                .Where(s => s.SubscriberProfile != null
                                         && s.MsisdnAsset != null
                                         && s.MsisdnAsset.Msisdn != null
                                         && (s.SubscriberProfile.OperationalStatus == SubscriberOperationalStatus.Suspended
                                          || s.SubscriberProfile.OperationalStatus == SubscriberOperationalStatus.SuspendedInbound
                                          || s.SubscriberProfile.OperationalStatus == SubscriberOperationalStatus.SuspendedOutbound))
                                .ToListAsync(ct);

                            if (suspendedSubs.Count == 0)
                            {
                                return;
                            }

                            var leakageCandidates = await scanner.ScanAsync(suspendedSubs, ct);
                            if (leakageCandidates.Count == 0)
                            {
                                return;
                            }

                            var msisdns = leakageCandidates
                                .Select(c => c.Subscription.MsisdnAsset!.Msisdn!)
                                .Distinct()
                                .ToList();

                            var openTicketMsisdns = await query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
                                .Where(t => t.Msisdn != null
                                            && msisdns.Contains(t.Msisdn)
                                            && t.TicketCategory == TechnicalTicketCategory.RevenueAssurance
                                            && (t.Status == TechnicalTicketStatus.Open || t.Status == TechnicalTicketStatus.InProgress))
                                .Select(t => t.Msisdn!)
                                .Distinct()
                                .ToListAsync(ct);

                            var openTicketSet = openTicketMsisdns.ToHashSet(StringComparer.Ordinal);

                            foreach (var candidate in leakageCandidates)
                            {
                                var sub = candidate.Subscription;
                                var profile = sub.SubscriberProfile;
                                var msisdnAsset = sub.MsisdnAsset;
                                if (profile == null || msisdnAsset?.Msisdn == null)
                                {
                                    continue;
                                }

                                var msisdn = msisdnAsset.Msisdn;

                                _logger.LogWarning(
                                    "Revenue Leakage detected for MSISDN {Msisdn}. CRM: {CrmStatus}, HLR: ACTIVE.",
                                    msisdn,
                                    profile.OperationalStatus);

                                if (openTicketSet.Contains(msisdn))
                                {
                                    continue;
                                }

                                var ticket = new TelecomTechnicalTicket
                                        {
                                            TicketNumber = $"RA-{DateTime.UtcNow:yyyyMMdd}-{msisdn.Substring(Math.Max(0, msisdn.Length - 4))}",
                                            Msisdn = msisdn,
                                            CustomerId = profile.CustomerId,
                                            SubscriberProfileId = sub.SubscriberProfileId,
                                            TicketCategory = TechnicalTicketCategory.RevenueAssurance,
                                            IssueType = TechnicalTicketIssueType.Network,
                                            Priority = TechnicalTicketPriority.High,
                                            Status = TechnicalTicketStatus.Open,
                                            Notes = $"Revenue Leakage Alert: CRM status is {profile.OperationalStatus} but HLR status is ACTIVE. Technical sync required to prevent unauthorized usage.",
                                            CreatedById = TechnicalTicketCreatedByChannel.RevenueAssuranceSystemUserId,
                                            OpenedByUserId = TechnicalTicketCreatedByChannel.RevenueAssuranceSystemUserId,
                                            CreatedByChannel = TechnicalTicketCreatedByChannel.SystemJob
                                        };

                                        await ticketRepo.CreateAsync(ticket, ct);
                                        await unitOfWork.SaveAsync(ct);

                                        _logger.LogInformation(
                                            "Created RevenueLeakageAlert ticket {TicketNumber} for {Msisdn}.",
                                            ticket.TicketNumber,
                                            msisdn);
                            }
                        },
                        stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RevenueAssuranceReconciliationJob iteration failed.");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
