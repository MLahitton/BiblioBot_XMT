namespace Api.Contracts.Inventory;

public sealed class RegisterInventoryExitRequest
{
    public Guid BookId { get; init; }
    public Guid BranchId { get; init; }
    public int Quantity { get; init; }
    public string? Reason { get; init; }
}

