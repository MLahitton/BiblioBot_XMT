using Domain.Common;

namespace Domain.Entities;

public class InventoryStock : BaseEntity
{
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public int CurrentStock { get; set; }
    public int MinStock { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
