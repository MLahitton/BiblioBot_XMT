using Domain.Common;

namespace Domain.Entities;

public class Sale : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid StatusId { get; set; }
    public SaleStatus Status { get; set; } = null!;
    public Guid OriginId { get; set; }
    public SaleOrigin Origin { get; set; } = null!;
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? RejectedReason { get; set; }

    public ICollection<SaleDetail> SaleDetails { get; set; } = [];
    public Invoice? Invoice { get; set; }
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
}
