using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Sales.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Sales.GetSales;

public sealed class GetSalesQueryHandler : IRequestHandler<GetSalesQuery, PagedResult<SaleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSalesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<SaleDto>> Handle(
        GetSalesQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _context.Sales.AsNoTracking()
            .Include(sale => sale.Status)
            .Include(sale => sale.Origin)
            .Include(sale => sale.Branch)
            .Include(sale => sale.Customer)
            .Include(sale => sale.Actor)
            .AsQueryable();

        if (request.CanReadAll)
        {
            if (request.CustomerId.HasValue)
            {
                query = query.Where(sale => sale.CustomerId == request.CustomerId.Value);
            }
        }
        else
        {
            query = query.Where(sale => sale.CustomerId == request.ActorId);
        }

        if (!string.IsNullOrWhiteSpace(request.StatusCode))
        {
            var statusCode = request.StatusCode.Trim().ToUpperInvariant();
            query = query.Where(sale => sale.Status.Code == statusCode);
        }

        if (!string.IsNullOrWhiteSpace(request.OriginCode))
        {
            var originCode = request.OriginCode.Trim().ToUpperInvariant();
            query = query.Where(sale => sale.Origin.Code == originCode);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(sale => sale.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(sale => new SaleDto
            {
                Id = sale.Id,
                CustomerId = sale.CustomerId,
                CustomerName = sale.Customer?.FullName,
                ActorId = sale.ActorId,
                ActorName = sale.Actor?.FullName,
                BranchId = sale.BranchId,
                BranchName = sale.Branch != null ? sale.Branch.Name : null,
                StatusCode = sale.Status.Code,
                StatusName = sale.Status.Name,
                OriginCode = sale.Origin.Code,
                OriginName = sale.Origin.Name,
                Subtotal = sale.Subtotal,
                TaxTotal = sale.TaxTotal,
                Total = sale.Total,
                CreatedAt = sale.CreatedAt,
                ConfirmedAt = sale.ConfirmedAt,
                IsIdempotent = false,
                Details = [],
                Invoice = null,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<SaleDto>(items, pageNumber, pageSize, totalCount);
    }
}
