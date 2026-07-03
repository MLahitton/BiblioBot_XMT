using Application.Features.Cart.Common;
using MediatR;

namespace Application.Features.Cart.RemoveCartItem;

public sealed class RemoveCartItemCommand : IRequest<CartDto?>
{
    public string SessionId { get; init; } = string.Empty;
    public Guid BookId { get; init; }
}

