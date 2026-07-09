namespace Application.Features.Inventory.Common;

public sealed class InventoryOperationResultDto
{
    public Guid InventoryStockId { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public int PreviousStock { get; init; }
    public int NewStock { get; init; }
    public int MinStock { get; init; }
    public string MovementTypeCode { get; init; } = string.Empty;
    public Guid? MovementId { get; init; }
    public string? Reason { get; init; }
}

