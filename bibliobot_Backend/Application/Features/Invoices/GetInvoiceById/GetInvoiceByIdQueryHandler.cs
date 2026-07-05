using Application.Common.Interfaces;
using Application.Features.Invoices.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Invoices.GetInvoiceById;

public sealed class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetInvoiceByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InvoiceDetailDto?> Handle(
        GetInvoiceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices.AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Sale)
                .ThenInclude(sale => sale.Status)
            .Include(i => i.Sale)
                .ThenInclude(sale => sale.Origin)
            .Include(i => i.Sale)
                .ThenInclude(sale => sale.Branch)
            .Include(i => i.Sale)
                .ThenInclude(sale => sale.SaleDetails)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        if (!request.CanReadAll && invoice.CustomerId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("No tienes permisos para consultar esta factura.");
        }

        return new InvoiceDetailDto
        {
            Id = invoice.Id,
            SaleId = invoice.SaleId,
            InvoiceNumber = invoice.InvoiceNumber,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer.FullName,
            Subtotal = invoice.Subtotal,
            TaxTotal = invoice.TaxTotal,
            Total = invoice.Total,
            IssuedAt = invoice.IssuedAt,
            IsCancelled = invoice.IsCancelled,
            CancelledAt = invoice.CancelledAt,
            Sale = invoice.Sale is null
                ? null!
                : new InvoiceSaleDetailDto
                {
                    SaleId = invoice.Sale.Id,
                    StatusCode = invoice.Sale.Status?.Code ?? string.Empty,
                    StatusName = invoice.Sale.Status?.Name,
                    OriginCode = invoice.Sale.Origin?.Code ?? string.Empty,
                    OriginName = invoice.Sale.Origin?.Name,
                    BranchId = invoice.Sale.BranchId,
                    BranchName = invoice.Sale.Branch?.Name,
                    SaleCreatedAt = invoice.Sale.CreatedAt,
                    ConfirmedAt = invoice.Sale.ConfirmedAt,
                },
            Items = invoice.Sale is null
                ? Array.Empty<InvoiceBookItemDto>()
                : invoice.Sale.SaleDetails
                    .Select(detail => new InvoiceBookItemDto
                    {
                        SaleDetailId = detail.Id,
                        BookId = detail.BookId,
                        BookTitleSnapshot = detail.BookTitleSnapshot,
                        IsbnSnapshot = detail.IsbnSnapshot,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice,
                        LineTotal = detail.LineTotal,
                    })
                    .ToList(),
        };
    }
}
