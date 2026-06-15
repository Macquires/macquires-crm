using Application.Common.Integrations;

namespace Infrastructure.TelecomIntegrations;

public sealed class BillingPostingMockIntegration : IBillingPostingIntegration
{
    public Task<BillingJournalPostResult> PostJournalEntryAsync(
        BillingJournalPostRequest request,
        CancellationToken cancellationToken = default)
    {
        var journalId = $"JE-{request.Kind}-{Guid.NewGuid():N}"[..32];
        return Task.FromResult(new BillingJournalPostResult(
            true,
            $"Posted journal {request.JournalCode} (mock).",
            journalId));
    }
}
