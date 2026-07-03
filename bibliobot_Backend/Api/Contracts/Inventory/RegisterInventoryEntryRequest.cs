namespace Api.Contracts.Inventory;

public sealed class RegisterInventoryEntryRequest
{
    public Guid BookId { get; init; }
    public Guid BranchId { get; init; }
    public int Quantity { get; init; }
    public string? Reason { get; init; }
    public int? MinStock { get; init; }
}

