using Domain.Common;

namespace Domain.Entities;

public class Branch : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<InventoryStock> InventoryStocks { get; set; } = [];
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
    public ICollection<Sale> SalesAsBranch { get; set; } = [];
    public ICollection<InternalRequest> SourceInternalRequests { get; set; } = [];
    public ICollection<InternalRequest> TargetInternalRequests { get; set; } = [];
}
