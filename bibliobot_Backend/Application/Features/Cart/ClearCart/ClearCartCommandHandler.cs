using Application.Common.Interfaces;
using Application.Features.Cart.Common;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Cart.ClearCart;

public sealed class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, CartDto?>
{
    private readonly IApplicationDbContext _context;

    public ClearCartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartDto?> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId.Trim();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var cart = await _context.Carts
            .Include(cart => cart.CartItems)
            .ThenInclude(item => item.Book)
            .FirstOrDefaultAsync(
                cart => cart.SessionId == sessionId && cart.Status == CartStatusCodes.Active,
                cancellationToken);

        if (cart is null)
        {
            return null;
        }

        _context.CartItems.RemoveRange(cart.CartItems);
        cart.Status = CartStatusCodes.Cancelled;
        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return BuildCartDto(cart);
    }

    private static CartDto BuildCartDto(Domain.Entities.Cart cart)
    {
        return new CartDto
        {
            Id = cart.Id,
            SessionId = cart.SessionId,
            UserId = cart.UserId,
            Status = cart.Status,
            Items = [],
            TotalItems = 0,
            Subtotal = 0,
        };
    }
}
