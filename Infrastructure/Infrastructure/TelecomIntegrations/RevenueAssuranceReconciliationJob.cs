using Application.Common.CQS.Queries;
using Application.Common.Extensions;
using Application.Common.Integrations;
using Application.Common.Repositories;
using Application.Common.Telecom;
using Domain.Entities;
using Domain.Enums;
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
                var query = scope.ServiceProvider.GetRequiredService<IQueryContext>();
                var hlr = scope.ServiceProvider.GetRequiredService<IHLRLiveStatusService>();
                var ticketRepo = scope.ServiceProvider.GetRequiredService<ICommandRepository<TelecomTechnicalTicket>>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // 1. Scan suspended subscriptions (potential leakage if HLR is still active)
                var suspendedSubs = await query.TelecomSubscription
                    .Include(s => s.MsisdnAsset)
                    .Include(s => s.SubscriberProfile)
                    .Where(s => s.SubscriberProfile.OperationalStatus == SubscriberOperationalStatus.Suspended
                             || s.SubscriberProfile.OperationalStatus == SubscriberOperationalStatus.SuspendedInbound
                             || s.SubscriberProfile.OperationalStatus == SubscriberOperationalStatus.SuspendedOutbound)
                    .ToListAsync(stoppingToken);

                foreach (var sub in suspendedSubs)
                {
                    if (string.IsNullOrEmpty(sub.MsisdnAsset?.Msisdn)) continue;

                    // 2. Query HLR Live Status
                    var hlrStatus = await hlr.QueryLiveStatusAsync(
                        sub.MsisdnAsset.Msisdn, 
                        sub.SubscriberProfile.OperationalStatus.ToString(), 
                        stoppingToken);

                    // 3. CONDITION FOR ALERT: IF CRM/CBS Billing Status == Suspended AND HLR Live Status == ACTIVE
                    if (hlrStatus.Success && hlrStatus.IsOnline && hlrStatus.HlrSubscriberState == "ACTIVE")
                    {
                        _logger.LogWarning("Revenue Leakage detected for MSISDN {Msisdn}. CRM: {CrmStatus}, HLR: ACTIVE.", 
                            sub.MsisdnAsset.Msisdn, sub.SubscriberProfile.OperationalStatus);

                        // 4. ACTION REQUIRED: Spawn a TelecomTechnicalTicket (RevenueAssurance)
                        // Check if an open ticket already exists for this MSISDN and category
                        var openExists = await query.TelecomTechnicalTicket.AsNoTracking().IsDeletedEqualTo()
                            .AnyAsync(t => t.Msisdn == sub.MsisdnAsset.Msisdn 
                                           && t.TicketCategory == TechnicalTicketCategory.RevenueAssurance
                                           && (t.Status == TechnicalTicketStatus.Open || t.Status == TechnicalTicketStatus.InProgress), 
                                      stoppingToken);

                        if (!openExists)
                        {
                            var ticket = new TelecomTechnicalTicket
                            {
                                TicketNumber = $"RA-{DateTime.UtcNow:yyyyMMdd}-{sub.MsisdnAsset.Msisdn.Substring(Math.Max(0, sub.MsisdnAsset.Msisdn.Length - 4))}",
                                Msisdn = sub.MsisdnAsset.Msisdn,
                                CustomerId = sub.SubscriberProfile.CustomerId,
                                SubscriberProfileId = sub.SubscriberProfileId,
                                TicketCategory = TechnicalTicketCategory.RevenueAssurance,
                                IssueType = TechnicalTicketIssueType.Network,
                                Priority = TechnicalTicketPriority.High,
                                Status = TechnicalTicketStatus.Open,
                                Notes = $"Revenue Leakage Alert: CRM status is {sub.SubscriberProfile.OperationalStatus} but HLR status is ACTIVE. Technical sync required to prevent unauthorized usage.",
                                CreatedById = TechnicalTicketCreatedByChannel.RevenueAssuranceSystemUserId,
                                OpenedByUserId = TechnicalTicketCreatedByChannel.RevenueAssuranceSystemUserId,
                                CreatedByChannel = TechnicalTicketCreatedByChannel.SystemJob
                            };

                            await ticketRepo.CreateAsync(ticket, stoppingToken);
                            await unitOfWork.SaveAsync(stoppingToken);
                            
                            _logger.LogInformation("Created RevenueLeakageAlert ticket {TicketNumber} for {Msisdn}.", ticket.TicketNumber, sub.MsisdnAsset.Msisdn);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RevenueAssuranceReconciliationJob iteration failed.");
            }

            // Run every 12 hours
            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
