using Application.Common.Interfaces;
using Application.Features.Reports.Common;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reports.GetTopSellingBooksReport;

public sealed class GetTopSellingBooksReportQueryHandler :
    IRequestHandler<GetTopSellingBooksReportQuery, IReadOnlyCollection<TopSellingBookDto>>
{
    private readonly IApplicationDbContext _context;

    public GetTopSellingBooksReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<TopSellingBookDto>> Handle(
        GetTopSellingBooksReportQuery request,
        CancellationToken cancellationToken)
    {
        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            throw new ArgumentException("El rango de fechas es inválido. 'from' debe ser menor o igual a 'to'.");
        }

        if (request.Limit < 1 || request.Limit > 50)
        {
            throw new ArgumentException("El parámetro limit debe estar entre 1 y 50.");
        }

        var query = _context.SaleDetails.AsNoTracking()
            .Include(detail => detail.Sale)
            .Include(detail => detail.Book)
            .Include(detail => detail.Sale.Status)
            .Where(detail => detail.Sale.Status.Code == SaleStatusCodes.Confirmed)
            .Where(detail => !detail.Book.IsDeleted)
            .AsQueryable();

        if (request.From.HasValue)
        {
            query = query.Where(detail => detail.Sale.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(detail => detail.Sale.CreatedAt <= request.To.Value);
        }

        if (request.BranchId.HasValue)
        {
            query = query.Where(detail => detail.Sale.BranchId == request.BranchId.Value);
        }

        var items = await query
            .GroupBy(detail => new
            {
                detail.BookId,
                BookTitle = detail.Book.Title,
                detail.IsbnSnapshot,
            })
            .Select(group => new TopSellingBookDto
            {
                BookId = group.Key.BookId,
                BookTitle = group.Key.BookTitle,
                Isbn = group.Key.IsbnSnapshot,
                UnitsSold = group.Sum(detail => detail.Quantity),
                Revenue = group.Sum(detail => detail.LineTotal),
                SalesCount = group.Select(detail => detail.SaleId).Distinct().Count(),
            })
            .OrderByDescending(item => item.UnitsSold)
            .ThenByDescending(item => item.Revenue)
            .ThenBy(item => item.BookTitle)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return items;
    }
}

