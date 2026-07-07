namespace Api.Contracts.Inventory;

public sealed class RegisterInventoryAdjustmentRequest
{
    public Guid BookId { get; init; }
    public Guid BranchId { get; init; }
    public int NewStock { get; init; }
    public string? Reason { get; init; }
    public int? MinStock { get; init; }
}

