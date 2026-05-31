namespace Application.Common.Services.SecurityManager;

public class MenuBadgesDto
{
    /// <summary>Telecom Hub — operations awaiting documents or confirmation.</summary>
    public int PendingOperations { get; init; }

    /// <summary>Back-office dashboard — critical tickets open &gt; 4h.</summary>
    public int OverdueTickets { get; init; }

    /// <summary>Technical ticket list — open or in-progress tickets.</summary>
    public int OpenTechnicalTickets { get; init; }

    /// <summary>Bulk import monitor — jobs pending or processing.</summary>
    public int BulkImportActive { get; init; }

    public IReadOnlyDictionary<string, int> ToDictionary() => new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["pendingOperations"] = PendingOperations,
        ["overdueTickets"] = OverdueTickets,
        ["openTechnicalTickets"] = OpenTechnicalTickets,
        ["bulkImportActive"] = BulkImportActive,
    };
}
