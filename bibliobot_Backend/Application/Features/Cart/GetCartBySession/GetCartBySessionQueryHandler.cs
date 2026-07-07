using Application.Common.Interfaces;
using Application.Features.Cart.Common;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Cart.GetCartBySession;

public sealed class GetCartBySessionQueryHandler : IRequestHandler<GetCartBySessionQuery, CartDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCartBySessionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CartDto?> Handle(
        GetCartBySessionQuery request,
        CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId.Trim();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new CartDto
            {
                SessionId = sessionId,
                Status = CartStatusCodes.Active,
                Items = [],
                TotalItems = 0,
                Subtotal = 0,
            };
        }

        var cart = await _context.Carts
            .AsNoTracking()
            .Where(cart => cart.SessionId == sessionId && cart.Status == CartStatusCodes.Active)
            .Select(cart => new CartDto
            {
                Id = cart.Id,
                SessionId = cart.SessionId,
                UserId = cart.UserId,
                Status = cart.Status,
                Items = cart.CartItems
                    .Select(item => new CartItemDto
                    {
                        Id = item.Id,
                        BookId = item.BookId,
                        BookTitle = item.Book.Title,
                        Isbn = item.Book.Isbn,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        LineTotal = item.Quantity * item.UnitPrice,
                        ImageUrl = item.Book.ImageUrl,
                    })
                    .ToList(),
                TotalItems = cart.CartItems.Sum(item => item.Quantity),
                Subtotal = cart.CartItems.Sum(item => item.Quantity * item.UnitPrice),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (cart is not null)
        {
            return cart;
        }

        return new CartDto
        {
            SessionId = sessionId,
            Status = CartStatusCodes.Active,
            Items = [],
            TotalItems = 0,
            Subtotal = 0,
        };
    }
}
