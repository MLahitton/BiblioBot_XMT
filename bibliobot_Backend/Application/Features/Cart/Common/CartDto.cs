namespace Application.Features.Cart.Common;

public sealed class CartDto
{
    public Guid? Id { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public IReadOnlyCollection<CartItemDto> Items { get; init; } = [];
    public int TotalItems { get; init; }
    public decimal Subtotal { get; init; }
}

