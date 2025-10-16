using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class DebitNote : BaseEntity
{
    public string? Number { get; set; }
    public DateTime? DebitNoteDate { get; set; }
    public DebitNoteStatus? DebitNoteStatus { get; set; }
    public string? Description { get; set; }
    public string? PurchaseReturnId { get; set; }
    public PurchaseReturn? PurchaseReturn { get; set; }
}
