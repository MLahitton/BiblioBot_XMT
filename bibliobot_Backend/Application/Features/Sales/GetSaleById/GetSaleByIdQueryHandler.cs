using Application.Common.Interfaces;
using Application.Features.Sales.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Sales.GetSaleById;

public sealed class GetSaleByIdQueryHandler : IRequestHandler<GetSaleByIdQuery, SaleDto?>
{
    private readonly IApplicationDbContext _context;

    public GetSaleByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SaleDto?> Handle(
        GetSaleByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Sales.AsNoTracking()
            .Include(sale => sale.Status)
            .Include(sale => sale.Origin)
            .Include(sale => sale.Branch)
            .Include(sale => sale.Invoice)
            .Include(sale => sale.Customer)
            .Include(sale => sale.Actor)
            .Include(sale => sale.SaleDetails)
            .Where(sale => sale.Id == request.Id);

        var sale = await query.FirstOrDefaultAsync(cancellationToken);

        if (sale is null)
        {
            return null;
        }

        if (!request.CanReadAll && sale.CustomerId != request.ActorId)
        {
            throw new UnauthorizedAccessException("No tienes permisos para ver esta venta.");
        }

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
                },
        };
    }
}
