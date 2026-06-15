using Application.Common.Integrations;
using Application.Common.Telecom.RevenueAssurance;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.TelecomIntegrations;

public sealed class RevenueAssuranceLeakageScanner : IRevenueAssuranceLeakageScanner
{
    private readonly IHLRLiveStatusService _hlr;
    private readonly int _maxConcurrency;

    public RevenueAssuranceLeakageScanner(IHLRLiveStatusService hlr, IConfiguration configuration)
    {
        _hlr = hlr;
        _maxConcurrency = configuration.GetValue("RevenueAssurance:HlrMaxConcurrency", 5);
        _maxConcurrency = Math.Clamp(_maxConcurrency, 1, 20);
    }

    public async Task<IReadOnlyList<RevenueAssuranceLeakageCandidate>> ScanAsync(
        IReadOnlyList<TelecomSubscription> suspendedSubscriptions,
        CancellationToken cancellationToken)
    {
        if (suspendedSubscriptions.Count == 0)
        {
            return Array.Empty<RevenueAssuranceLeakageCandidate>();
        }

        using var gate = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
        var candidates = new List<RevenueAssuranceLeakageCandidate>();
        var sync = new object();

        var tasks = suspendedSubscriptions
            .Where(s => !string.IsNullOrEmpty(s.MsisdnAsset?.Msisdn) && s.SubscriberProfile != null)
            .Select(async sub =>
            {
                await gate.WaitAsync(cancellationToken);
                try
                {
                    var profile = sub.SubscriberProfile!;
                    var msisdn = sub.MsisdnAsset!.Msisdn!;
                    var hlrStatus = await _hlr.QueryLiveStatusAsync(
                        msisdn,
                        profile.OperationalStatus.ToString(),
                        cancellationToken);

                    if (hlrStatus.Success
                        && hlrStatus.IsOnline
                        && hlrStatus.HlrSubscriberState == "ACTIVE")
                    {
                        lock (sync)
                        {
                            candidates.Add(new RevenueAssuranceLeakageCandidate(sub, hlrStatus));
                        }
                    }
                }
                finally
                {
                    gate.Release();
                }
            });

        await Task.WhenAll(tasks);
        return candidates;
    }
}
