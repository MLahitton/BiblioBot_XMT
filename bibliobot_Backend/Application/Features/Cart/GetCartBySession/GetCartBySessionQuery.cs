using Application.Features.Cart.Common;
using MediatR;

namespace Application.Features.Cart.GetCartBySession;

public sealed class GetCartBySessionQuery : IRequest<CartDto?>
{
    public string SessionId { get; init; } = string.Empty;
}

