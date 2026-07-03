namespace Api.Contracts.Cart;

public sealed class AddOrUpdateCartItemRequest
{
    public string SessionId { get; init; } = string.Empty;
    public Guid BookId { get; init; }
    public int Quantity { get; init; }
    public Guid? BranchId { get; init; }
}

