using Application.Common.Interfaces;
using Application.Features.Sales.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Sales.CreateSale;

public sealed class CreateSaleCommandHandler : IRequestHandler<CreateSaleCommand, SaleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateSaleCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<SaleDto> Handle(CreateSaleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var actorId = _currentUserService.UserId.Value;
        var actor = await _context.Users.FirstOrDefaultAsync(
            user => user.Id == actorId,
            cancellationToken);

        if (actor is null)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var sessionId = request.SessionId.Trim();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("SessionId requerido.");
        }

        if (sessionId.Length > 120)
        {
            throw new ArgumentException("SessionId máximo 120 caracteres.");
        }

        var rawOriginCode = request.OriginCode?.Trim().ToUpperInvariant();
        var originCode = string.IsNullOrWhiteSpace(rawOriginCode) ? SaleOriginCodes.WebUi : rawOriginCode;

        if (originCode != SaleOriginCodes.WebUi && originCode != SaleOriginCodes.Chatbot)
        {
            throw new ArgumentException("Código de origen inválido.");
        }

        if (request.BranchId.HasValue)
        {
            var branchExists = await _context.Branches.AnyAsync(
                branch => branch.Id == request.BranchId.Value && branch.IsActive,
                cancellationToken);

            if (!branchExists)
            {
                throw new KeyNotFoundException("Sede no encontrada.");
            }
        }

        var cart = await _context.Carts
            .Include(cart => cart.CartItems)
            .ThenInclude(item => item.Book)
            .FirstOrDefaultAsync(cart => cart.SessionId == sessionId, cancellationToken);

        if (cart is null)
        {
            throw new KeyNotFoundException("Carrito no encontrado.");
        }

        if (cart.Status != CartStatusCodes.Active)
        {
            throw new InvalidOperationException("El carrito no está activo.");
        }

        if (!cart.CartItems.Any())
        {
            throw new InvalidOperationException("El carrito está vacío.");
        }

        if (cart.UserId.HasValue && cart.UserId.Value != actorId)
        {
            throw new UnauthorizedAccessException("No tienes permisos para este carrito.");
        }
        
        if (!cart.UserId.HasValue)
        {
            cart.UserId = actorId;
        }

        foreach (var item in cart.CartItems)
        {
            if (item.Book is null || !item.Book.IsActive || item.Book.IsDeleted)
            {
                throw new KeyNotFoundException("Libro no encontrado.");
            }
        }

        var saleStatus = await _context.SaleStatuses.FirstOrDefaultAsync(
            status => status.Code == SaleStatusCodes.PendingConfirmation,
            cancellationToken);

        if (saleStatus is null)
        {
            saleStatus = await _context.SaleStatuses.FirstOrDefaultAsync(
                status => status.Code == SaleStatusCodes.Created,
                cancellationToken);
        }

        if (saleStatus is null)
        {
            throw new KeyNotFoundException("Estado de venta no encontrado.");
        }

        var saleOrigin = await _context.SaleOrigins.FirstOrDefaultAsync(
            origin => origin.Code == originCode,
            cancellationToken);

        if (saleOrigin is null)
        {
            throw new KeyNotFoundException("Origen de venta no encontrado.");
        }

        var now = DateTimeOffset.UtcNow;
        var details = new List<SaleDetail>();
        var subtotal = 0m;

        foreach (var item in cart.CartItems)
        {
            var lineTotal = item.UnitPrice * item.Quantity;

            details.Add(new SaleDetail
            {
                BookId = item.BookId,
                BookTitleSnapshot = item.Book?.Title ?? string.Empty,
                IsbnSnapshot = item.Book?.Isbn,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = lineTotal,
            });

            subtotal += lineTotal;
        }

        var sale = new Sale
        {
            CustomerId = actorId,
            ActorId = actorId,
            BranchId = request.BranchId,
            StatusId = saleStatus.Id,
            OriginId = saleOrigin.Id,
            Subtotal = subtotal,
            TaxTotal = 0m,
            Total = subtotal,
            CreatedAt = now,
            UpdatedAt = now,
            SaleDetails = details,
        };

        _context.Sales.Add(sale);
        await _context.SaveChangesAsync(cancellationToken);

        return MapSaleToDto(
            sale,
            saleStatus.Code,
            saleStatus.Name,
            saleOrigin.Code,
            saleOrigin.Name,
            details,
            actor.FullName,
            actor.FullName);
    }

    private static SaleDto MapSaleToDto(
        Sale sale,
        string statusCode,
        string statusName,
        string originCode,
        string originName,
        IReadOnlyCollection<SaleDetail> details,
        string customerName,
        string actorName)
    {
        return new SaleDto
        {
            Id = sale.Id,
            CustomerId = sale.CustomerId,
            CustomerName = customerName,
            ActorId = sale.ActorId,
            ActorName = actorName,
            BranchId = sale.BranchId,
            StatusCode = statusCode,
            StatusName = statusName,
            OriginCode = originCode,
            OriginName = originName,
            Subtotal = sale.Subtotal,
            TaxTotal = sale.TaxTotal,
            Total = sale.Total,
            CreatedAt = sale.CreatedAt,
            ConfirmedAt = sale.ConfirmedAt,
            Details = details
                .Select(detail => new SaleDetailDto
                {
                    Id = detail.Id,
                    BookId = detail.BookId,
                    BookTitleSnapshot = detail.BookTitleSnapshot,
                    IsbnSnapshot = detail.IsbnSnapshot,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    LineTotal = detail.LineTotal
                })
                .ToList(),
        };
    }
}
