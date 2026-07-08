using Application.Common.Interfaces;
using Application.Features.Cart.Common;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Cart.ClearCart;

public sealed class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, CartDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ClearCartCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CartDto?> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId.Trim();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var currentUserId = _currentUserService.IsAuthenticated
            ? _currentUserService.UserId
            : null;

        var cart = await _context.Carts
            .Include(cart => cart.CartItems)
            .ThenInclude(item => item.Book)
            .Where(cart =>
                cart.Status == CartStatusCodes.Active &&
                (
                    (currentUserId.HasValue && cart.UserId == currentUserId.Value) ||
                    (cart.SessionId == sessionId && (!cart.UserId.HasValue || cart.UserId == currentUserId))
                ))
            .OrderByDescending(cart => currentUserId.HasValue && cart.UserId == currentUserId.Value)
            .ThenByDescending(cart => cart.UpdatedAt ?? cart.CreatedAt)
            .FirstOrDefaultAsync(
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
