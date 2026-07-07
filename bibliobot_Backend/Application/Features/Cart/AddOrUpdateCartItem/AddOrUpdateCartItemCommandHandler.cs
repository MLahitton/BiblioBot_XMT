using Application.Common.Interfaces;
using Application.Features.Cart.Common;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Cart.AddOrUpdateCartItem;

public sealed class AddOrUpdateCartItemCommandHandler : IRequestHandler<
    AddOrUpdateCartItemCommand,
    (CartDto Cart, bool IsCreated)>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddOrUpdateCartItemCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<(CartDto Cart, bool IsCreated)> Handle(
        AddOrUpdateCartItemCommand request,
        CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId.Trim();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("SessionId requerido.");
        }

        if (sessionId.Length > 120)
        {
            throw new ArgumentException("SessionId máximo 120 caracteres.");
        }

        if (request.Quantity <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor que 0.");
        }

        var book = await _context.Books.AsNoTracking()
            .FirstOrDefaultAsync(
                book => book.Id == request.BookId && book.IsActive && !book.IsDeleted,
                cancellationToken);

        if (book is null)
        {
            throw new KeyNotFoundException("Libro no encontrado.");
        }

        var branchId = request.BranchId;
        int availableStock;

        if (branchId.HasValue)
        {
            var branch = await _context.Branches.FirstOrDefaultAsync(
                branch => branch.Id == branchId.Value && branch.IsActive,
                cancellationToken);

            if (branch is null)
            {
                throw new KeyNotFoundException("Sede no encontrada.");
            }

            availableStock = await _context.InventoryStocks
                .Where(stock => stock.BookId == request.BookId && stock.BranchId == branchId.Value)
                .SumAsync(stock => stock.CurrentStock, cancellationToken);
        }
        else
        {
            availableStock = await _context.InventoryStocks
                .Where(stock => stock.BookId == request.BookId)
                .SumAsync(stock => stock.CurrentStock, cancellationToken);
        }

        if (availableStock < request.Quantity)
        {
            throw new InvalidOperationException("Stock insuficiente para agregar el libro al carrito.");
        }

        var cart = await _context.Carts
            .Include(cart => cart.CartItems)
                .ThenInclude(item => item.Book)
            .FirstOrDefaultAsync(
                cart => cart.SessionId == sessionId && cart.Status == CartStatusCodes.Active,
                cancellationToken);

        var isCreated = false;

        if (cart is null)
        {
            cart = new Domain.Entities.Cart
            {
                SessionId = sessionId,
                Status = CartStatusCodes.Active,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            if (_currentUserService.IsAuthenticated && _currentUserService.UserId.HasValue)
            {
                cart.UserId = _currentUserService.UserId.Value;
            }

            _context.Carts.Add(cart);
            isCreated = true;
        }
        else if (_currentUserService.IsAuthenticated && _currentUserService.UserId.HasValue)
        {
            cart.UserId = _currentUserService.UserId.Value;
        }

        var existingItem = cart.CartItems.FirstOrDefault(item => item.BookId == request.BookId);

        if (existingItem is null)
        {
            cart.CartItems.Add(new Domain.Entities.CartItem
            {
                BookId = request.BookId,
                Quantity = request.Quantity,
                UnitPrice = book.Price,
            });

            isCreated = true;
        }
        else
        {
            existingItem.Quantity = request.Quantity;
            existingItem.UpdatedAt = DateTimeOffset.UtcNow;
        }

        cart.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        var persistedCart = await _context.Carts
            .AsNoTracking()
            .Include(c => c.CartItems)
                .ThenInclude(item => item.Book)
            .FirstAsync(
                c => c.Id == cart.Id && c.Status == CartStatusCodes.Active,
                cancellationToken);

        var result = BuildCartDto(persistedCart);

        return (result, isCreated);
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
