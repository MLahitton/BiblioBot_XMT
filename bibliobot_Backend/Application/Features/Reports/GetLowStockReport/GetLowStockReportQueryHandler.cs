using Application.Common.Interfaces;
using Application.Features.Reports.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reports.GetLowStockReport;

public sealed class GetLowStockReportQueryHandler :
    IRequestHandler<GetLowStockReportQuery, IReadOnlyCollection<LowStockBookDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLowStockReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<LowStockBookDto>> Handle(
        GetLowStockReportQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Limit < 1 || request.Limit > 200)
        {
            throw new ArgumentException("El parámetro limit debe estar entre 1 y 200.");
        }

        var query = _context.InventoryStocks.AsNoTracking()
            .Include(stock => stock.Book)
            .Include(stock => stock.Branch)
            .Where(stock => stock.CurrentStock <= stock.MinStock)
            .Where(stock => !stock.Book.IsDeleted)
            .AsQueryable();

        if (request.BranchId.HasValue)
        {
            query = query.Where(stock => stock.BranchId == request.BranchId.Value);
        }

        var items = await query
            .OrderByDescending(stock => stock.MinStock - stock.CurrentStock)
            .ThenBy(stock => stock.CurrentStock)
            .Take(request.Limit)
            .Select(stock => new LowStockBookDto
            {
                BookId = stock.BookId,
                BookTitle = stock.Book.Title,
                Isbn = stock.Book.Isbn,
                BranchId = stock.BranchId,
                BranchName = stock.Branch.Name,
                CurrentStock = stock.CurrentStock,
                MinimumStock = stock.MinStock,
                Difference = stock.MinStock - stock.CurrentStock,
            })
            .ToListAsync(cancellationToken);

        return items;
    }
}

