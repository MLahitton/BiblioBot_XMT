using Application.Common.Interfaces;
using Application.Features.Cart.Common;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Cart.RemoveCartItem;

public sealed class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, CartDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveCartItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<CartDto?> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
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

        var item = cart.CartItems.FirstOrDefault(item => item.BookId == request.BookId);

        if (item is null)
        {
            return null;
        }

        cart.CartItems.Remove(item);
        _context.CartItems.Remove(item);

        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return BuildCartDto(cart);
    }

    private static CartDto BuildCartDto(Domain.Entities.Cart cart)
    {
        var items = cart.CartItems
            .Select(item => new CartItemDto
            {
                Id = item.Id,
                BookId = item.BookId,
                BookTitle = item.Book?.Title ?? string.Empty,
                Isbn = item.Book?.Isbn,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.Quantity * item.UnitPrice,
                ImageUrl = item.Book?.ImageUrl,
            })
            .ToList();

        return new CartDto
        {
            Id = cart.Id,
            SessionId = cart.SessionId,
            UserId = cart.UserId,
            Status = cart.Status,
            Items = items,
            TotalItems = items.Sum(item => item.Quantity),
            Subtotal = items.Sum(item => item.LineTotal),
        };
    }
}
