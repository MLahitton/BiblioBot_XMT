using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchInvoices;

public sealed class SearchInvoicesLookupQueryHandler
    : IRequestHandler<SearchInvoicesLookupQuery, PagedResult<LookupInvoiceDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchInvoicesLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupInvoiceDto>> Handle(
        SearchInvoicesLookupQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.Invoices.AsNoTracking();

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            if (Guid.TryParse(q, out var saleId))
            {
                query = query.Where(invoice =>
                    invoice.InvoiceNumber.ToUpper().Contains(normalized) ||
                    invoice.Customer.FullName.ToUpper().Contains(normalized) ||
                    invoice.Customer.Email.ToUpper().Contains(normalized) ||
                    invoice.SaleId == saleId);
            }
            else
            {
                query = query.Where(invoice =>
                    invoice.InvoiceNumber.ToUpper().Contains(normalized) ||
                    invoice.Customer.FullName.ToUpper().Contains(normalized) ||
                    invoice.Customer.Email.ToUpper().Contains(normalized));
            }
        }

        var invoiceNumber = request.InvoiceNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(invoiceNumber))
        {
            var normalizedInvoiceNumber = invoiceNumber!.ToUpperInvariant();
            query = query.Where(invoice =>
                invoice.InvoiceNumber.ToUpper().Contains(normalizedInvoiceNumber));
        }

        var customerEmail = request.CustomerEmail?.Trim();
        if (!string.IsNullOrWhiteSpace(customerEmail))
        {
            var normalizedEmail = customerEmail!.ToUpperInvariant();
            query = query.Where(invoice => invoice.Customer.Email.ToUpper().Contains(normalizedEmail));
        }

        if (request.SaleId.HasValue)
        {
            query = query.Where(invoice => invoice.SaleId == request.SaleId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(invoice => invoice.IssuedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(invoice => new LookupInvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                SaleId = invoice.SaleId,
                CustomerName = invoice.Customer.FullName,
                CustomerEmail = invoice.Customer.Email,
                Total = invoice.Total,
                IssuedAt = invoice.IssuedAt,
                IsCancelled = invoice.IsCancelled,
                Label = $"FAC-{invoice.InvoiceNumber} - {invoice.Customer.Email} - {invoice.Total:0}",
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupInvoiceDto>(items, pageNumber, pageSize, totalCount);
    }
}

