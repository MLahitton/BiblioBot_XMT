using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchSales;

public sealed class SearchSalesLookupQueryHandler
    : IRequestHandler<SearchSalesLookupQuery, PagedResult<LookupSaleDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchSalesLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupSaleDto>> Handle(
        SearchSalesLookupQuery request,
        CancellationToken cancellationToken)
    {
        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            throw new ArgumentException("Rango de fechas invalido.");
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.Sales.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.StatusCode))
        {
            var normalizedStatusCode = request.StatusCode!.Trim().ToUpperInvariant();
            query = query.Where(sale => sale.Status.Code == normalizedStatusCode);
        }

        var customerEmail = request.CustomerEmail?.Trim();
        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            var normalizedEmail = customerEmail!.ToUpperInvariant();
            query = query.Where(sale =>
                sale.Customer.Email.ToUpper().Contains(normalizedEmail));
        }

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            if (Guid.TryParse(q, out var saleId))
            {
                query = query.Where(sale =>
                    sale.Id == saleId ||
                    sale.Customer.FullName.ToUpper().Contains(normalized) ||
                    sale.Customer.Email.ToUpper().Contains(normalized) ||
                    (sale.Invoice != null && sale.Invoice.InvoiceNumber.ToUpper().Contains(normalized)));
            }
            else
            {
                query = query.Where(sale =>
                    sale.Customer.FullName.ToUpper().Contains(normalized) ||
                    sale.Customer.Email.ToUpper().Contains(normalized) ||
                    (sale.Invoice != null && sale.Invoice.InvoiceNumber.ToUpper().Contains(normalized)));
            }
        }

        if (request.From.HasValue)
        {
            query = query.Where(sale => sale.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(sale => sale.CreatedAt <= request.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(sale => sale.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(sale => new LookupSaleDto
            {
                Id = sale.Id,
                CustomerName = sale.Customer.FullName,
                CustomerEmail = sale.Customer.Email,
                StatusCode = sale.Status.Code,
                OriginCode = sale.Origin.Code,
                Total = sale.Total,
                CreatedAt = sale.CreatedAt,
                ConfirmedAt = sale.ConfirmedAt,
                Label = BuildLabel(sale.Total, sale.Customer.Email, sale.Status.Code),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupSaleDto>(items, pageNumber, pageSize, totalCount);
    }

    private static string BuildLabel(decimal total, string customerEmail, string statusCode)
    {
        return $"Venta {total:0} - {customerEmail} - {statusCode}";
    }
}

