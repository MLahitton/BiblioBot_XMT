using Application.Features.Cart.Common;
using MediatR;

namespace Application.Features.Cart.ClearCart;

public sealed class ClearCartCommand : IRequest<CartDto?>
{
    public string SessionId { get; init; } = string.Empty;
}

