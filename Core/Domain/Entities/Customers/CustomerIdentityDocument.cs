using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class CustomerIdentityDocument : BaseEntity
{
    public string CustomerId { get; set; } = null!;
    public Customer? Customer { get; set; }

    public IdentityDocumentType DocumentType { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public DateTime? ExpiresUtc { get; set; }
    public string? FileDocumentId { get; set; }
    public FileDocument? FileDocument { get; set; }
}
