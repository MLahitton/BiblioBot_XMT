using Domain.Common;

namespace Domain.Entities;

public class InternalRequestItem : BaseEntity
{
    public Guid InternalRequestId { get; set; }
    public InternalRequest InternalRequest { get; set; } = null!;
    public Guid? BookId { get; set; }
    public Book? Book { get; set; }
    public string? RequestedTitle { get; set; }
    public int Quantity { get; set; }
    public string? Observations { get; set; }
}
