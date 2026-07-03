using Application.Features.Cart.Common;
using MediatR;

namespace Application.Features.Cart.AddOrUpdateCartItem;

public sealed class AddOrUpdateCartItemCommand : IRequest<(CartDto Cart, bool IsCreated)>
{
    public string SessionId { get; init; } = string.Empty;
    public Guid BookId { get; init; }
    public int Quantity { get; init; }
    public Guid? BranchId { get; init; }
}

