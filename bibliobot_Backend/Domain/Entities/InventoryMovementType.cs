using Domain.Common;

namespace Domain.Entities;

public class InventoryMovementType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
}
