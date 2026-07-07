namespace Api.Contracts.InternalRequests;

public sealed class InternalRequestItemRequest
{
    public Guid BookId { get; init; }
    public int Quantity { get; init; }
}
