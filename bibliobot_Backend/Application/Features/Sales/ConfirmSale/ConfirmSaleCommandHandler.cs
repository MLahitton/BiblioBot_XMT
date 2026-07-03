using Application.Common.Interfaces;
using Application.Features.Sales.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Sales.ConfirmSale;

public sealed class ConfirmSaleCommandHandler : IRequestHandler<ConfirmSaleCommand, SaleDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ConfirmSaleCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<SaleDto> Handle(ConfirmSaleCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var actorId = _currentUserService.UserId.Value;
        var now = DateTimeOffset.UtcNow;

        var sale = await _context.Sales
            .Include(sale => sale.Status)
            .Include(sale => sale.Origin)
            .Include(sale => sale.Invoice)
            .Include(sale => sale.Branch)
            .Include(sale => sale.Customer)
            .Include(sale => sale.Actor)
            .Include(sale => sale.SaleDetails)
                .ThenInclude(detail => detail.Book)
            .FirstOrDefaultAsync(sale => sale.Id == request.Id, cancellationToken);

        if (sale is null)
        {
            throw new KeyNotFoundException("Venta no encontrada.");
        }

        if (!sale.SaleDetails.Any())
        {
            throw new ArgumentException("La venta no tiene detalles.");
        }

        var saleStatusConfirmed = await _context.SaleStatuses.FirstOrDefaultAsync(
            status => status.Code == SaleStatusCodes.Confirmed,
            cancellationToken);

        if (saleStatusConfirmed is null)
        {
            throw new KeyNotFoundException("Estado de venta no encontrado.");
        }

        var statusCode = sale.Status?.Code;

        if (statusCode is null)
        {
            throw new KeyNotFoundException("Estado de venta no encontrado.");
        }

        if (statusCode == SaleStatusCodes.Confirmed)
        {
            return MapSaleToDto(sale, true);
        }

        if (statusCode == SaleStatusCodes.Cancelled || statusCode == SaleStatusCodes.Rejected)
        {
            throw new InvalidOperationException("La venta no se puede confirmar por su estado.");
        }

        if (statusCode != SaleStatusCodes.Created && statusCode != SaleStatusCodes.PendingConfirmation)
        {
            throw new InvalidOperationException("La venta no se puede confirmar por su estado.");
        }

        if (sale.BranchId is null)
        {
            throw new ArgumentException("La venta debe tener una sede para confirmación.");
        }

        var branchExists = await _context.Branches.AnyAsync(
            branch => branch.Id == sale.BranchId && branch.IsActive,
            cancellationToken);

        if (!branchExists)
        {
            throw new KeyNotFoundException("Sede no encontrada.");
        }

        var bookIds = sale.SaleDetails.Select(detail => detail.BookId).Distinct().ToList();
        var books = await _context.Books
            .Where(book => bookIds.Contains(book.Id))
            .ToListAsync(cancellationToken);

        foreach (var detail in sale.SaleDetails)
        {
            var book = books.FirstOrDefault(book => book.Id == detail.BookId);
            if (book is null || !book.IsActive || book.IsDeleted)
            {
                throw new KeyNotFoundException("Libro no encontrado.");
            }
        }

        var inventoryStocks = await _context.InventoryStocks
            .Where(stock => stock.BranchId == sale.BranchId.Value && bookIds.Contains(stock.BookId))
            .ToListAsync(cancellationToken);

        foreach (var detail in sale.SaleDetails)
        {
            var stock = inventoryStocks.FirstOrDefault(stock => stock.BookId == detail.BookId);
            if (stock is null)
            {
                throw new KeyNotFoundException("Stock no encontrado.");
            }

            if (stock.CurrentStock < detail.Quantity)
            {
                throw new InvalidOperationException("Stock insuficiente para confirmar la venta.");
            }
        }

        var saleMovementType = await _context.InventoryMovementTypes.FirstOrDefaultAsync(
            movementType => movementType.Code == InventoryMovementTypeCodes.Sale,
            cancellationToken);

        if (saleMovementType is null)
        {
            throw new KeyNotFoundException("Tipo de movimiento no encontrado.");
        }

        if (_context is Microsoft.EntityFrameworkCore.DbContext dbContext)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var invoiceNumber = await GetUniqueInvoiceNumberAsync(
                    sale.Id,
                    now,
                    cancellationToken);

                ApplyConfirmation(sale, now, actorId, saleMovementType, inventoryStocks, saleStatusConfirmed.Id);
                EnsureInvoice(sale, now, invoiceNumber);

                await _context.SaveChangesAsync(cancellationToken);
                await dbContext.Database.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await dbContext.Database.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            var invoiceNumber = await GetUniqueInvoiceNumberAsync(
                sale.Id,
                now,
                cancellationToken);

            ApplyConfirmation(sale, now, actorId, saleMovementType, inventoryStocks, saleStatusConfirmed.Id);
            EnsureInvoice(sale, now, invoiceNumber);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return MapSaleToDto(sale);
    }

    private static void ApplyConfirmation(
        Domain.Entities.Sale sale,
        DateTimeOffset now,
        Guid actorId,
        Domain.Entities.InventoryMovementType saleMovementType,
        List<Domain.Entities.InventoryStock> inventoryStocks,
        Guid confirmedStatusId)
    {
        foreach (var detail in sale.SaleDetails)
        {
            var stock = inventoryStocks.First(stock => stock.BookId == detail.BookId);
            var previousStock = stock.CurrentStock;
            var newStock = previousStock - detail.Quantity;

            stock.CurrentStock = newStock;
            stock.UpdatedAt = now;

            sale.InventoryMovements.Add(new InventoryMovement
            {
                BookId = detail.BookId,
                BranchId = sale.BranchId!.Value,
                MovementTypeId = saleMovementType.Id,
                Quantity = detail.Quantity,
                PreviousStock = previousStock,
                NewStock = newStock,
                Reason = "Venta confirmada",
                SaleId = sale.Id,
                ActorId = actorId,
                CreatedAt = now,
            });
        }

        sale.StatusId = confirmedStatusId;
        sale.ConfirmedAt = now;
        sale.UpdatedAt = now;
    }

    private static void EnsureInvoice(
        Domain.Entities.Sale sale,
        DateTimeOffset issuedAt,
        string invoiceNumber)
    {
        if (sale.Invoice is null)
        {
            sale.Invoice = new Invoice
            {
                SaleId = sale.Id,
                CustomerId = sale.CustomerId,
                InvoiceNumber = invoiceNumber,
                Subtotal = sale.Subtotal,
                TaxTotal = sale.TaxTotal,
                Total = sale.Total,
                IssuedAt = issuedAt,
                IsCancelled = false,
            };
        }
    }

    private static string GetInvoiceNumber(Guid saleId, DateTimeOffset issuedAt)
    {
        var salePrefix = saleId.ToString("N")[..8];
        return $"FAC-{issuedAt:yyyyMMddHHmmss}-{salePrefix}";
    }

    private async Task<string> GetUniqueInvoiceNumberAsync(
        Guid saleId,
        DateTimeOffset issuedAt,
        CancellationToken cancellationToken)
    {
        var number = issuedAt;
        var invoiceNumber = string.Empty;

        while (true)
        {
            invoiceNumber = GetInvoiceNumber(saleId, number);

            var exists = await _context.Invoices.AnyAsync(
                invoice => invoice.InvoiceNumber == invoiceNumber,
                cancellationToken);

            if (!exists)
            {
                return invoiceNumber;
            }

            number = number.AddSeconds(1);
        }
    }

    private static SaleDto MapSaleToDto(Domain.Entities.Sale sale, bool isIdempotent = false)
    {
        return new SaleDto
        {
            Id = sale.Id,
            CustomerId = sale.CustomerId,
            CustomerName = sale.Customer?.FullName,
            ActorId = sale.ActorId,
            ActorName = sale.Actor?.FullName,
            BranchId = sale.BranchId,
            BranchName = sale.Branch?.Name,
            StatusCode = sale.Status?.Code ?? string.Empty,
            StatusName = sale.Status?.Name,
            OriginCode = sale.Origin?.Code ?? string.Empty,
            OriginName = sale.Origin?.Name,
            Subtotal = sale.Subtotal,
            TaxTotal = sale.TaxTotal,
            Total = sale.Total,
            CreatedAt = sale.CreatedAt,
            ConfirmedAt = sale.ConfirmedAt,
            IsIdempotent = isIdempotent,
            Details = sale.SaleDetails
                .Select(detail => new SaleDetailDto
                {
                    Id = detail.Id,
                    BookId = detail.BookId,
                    BookTitleSnapshot = detail.BookTitleSnapshot,
                    IsbnSnapshot = detail.IsbnSnapshot,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    LineTotal = detail.LineTotal,
                })
                .ToList(),
            Invoice = sale.Invoice is null
                ? null
                : new SaleInvoiceDto
                {
                    Id = sale.Invoice.Id,
                    InvoiceNumber = sale.Invoice.InvoiceNumber,
                    Subtotal = sale.Invoice.Subtotal,
                    TaxTotal = sale.Invoice.TaxTotal,
                    Total = sale.Invoice.Total,
                    IssuedAt = sale.Invoice.IssuedAt,
                    IsCancelled = sale.Invoice.IsCancelled,
                }
        };
    }
}
