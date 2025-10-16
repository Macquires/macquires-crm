using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class CreditNote : BaseEntity
{
    public string? Number { get; set; }
    public DateTime? CreditNoteDate { get; set; }
    public CreditNoteStatus? CreditNoteStatus { get; set; }
    public string? Description { get; set; }
    public string? SalesReturnId { get; set; }
    public SalesReturn? SalesReturn { get; set; }
}
