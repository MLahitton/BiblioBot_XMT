using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Invoices.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Invoices.GetInvoices;

public sealed class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, PagedResult<InvoiceListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInvoicesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<InvoiceListItemDto>> Handle(
        GetInvoicesQuery request,
        CancellationToken cancellationToken)
    {
        if (!request.CanReadAll && !request.CanReadOwn)
        {
            throw new UnauthorizedAccessException("No tienes permisos para consultar facturas.");
        }

        if (request.CustomerId.HasValue && !request.CanReadAll && request.CustomerId.Value != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("No tienes permisos para consultar facturas ajenas.");
        }

        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            throw new ArgumentException("Rango de fechas invalido.");
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _context.Invoices.AsNoTracking();

        if (!request.CanReadAll)
        {
            query = query.Where(invoice => invoice.CustomerId == request.CurrentUserId);
        }
        else if (request.CustomerId.HasValue)
        {
            query = query.Where(invoice => invoice.CustomerId == request.CustomerId.Value);
        }

        if (request.SaleId.HasValue)
        {
            query = query.Where(invoice => invoice.SaleId == request.SaleId.Value);
        }

        if (request.IsCancelled.HasValue)
        {
            query = query.Where(invoice => invoice.IsCancelled == request.IsCancelled.Value);
        }

        if (request.From.HasValue)
        {
            query = query.Where(invoice => invoice.IssuedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(invoice => invoice.IssuedAt <= request.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(invoice => invoice.IssuedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(invoice => new InvoiceListItemDto
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
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InvoiceListItemDto>(items, pageNumber, pageSize, totalCount);
    }
}
