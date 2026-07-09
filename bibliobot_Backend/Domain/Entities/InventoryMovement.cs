using Domain.Common;

namespace Domain.Entities;

public class InventoryMovement : BaseEntity
{
    public Guid BookId { get; set; }
    public Book Book { get; set; } = null!;
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;
    public Guid MovementTypeId { get; set; }
    public InventoryMovementType MovementType { get; set; } = null!;
    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public string? Reason { get; set; }
    public Guid? SaleId { get; set; }
    public Sale? Sale { get; set; }
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
