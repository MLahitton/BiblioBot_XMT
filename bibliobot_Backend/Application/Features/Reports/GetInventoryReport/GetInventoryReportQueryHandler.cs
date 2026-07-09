using Application.Common.Interfaces;
using Application.Features.Reports.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reports.GetInventoryReport;

public sealed class GetInventoryReportQueryHandler : IRequestHandler<GetInventoryReportQuery, InventoryReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryReportDto> Handle(GetInventoryReportQuery request, CancellationToken cancellationToken)
    {
        var stockQuery = _context.InventoryStocks.AsNoTracking()
            .Include(stock => stock.Book)
            .Include(stock => stock.Branch)
            .Where(stock => !stock.Book.IsDeleted)
            .AsQueryable();

        if (request.BranchId.HasValue)
        {
            stockQuery = stockQuery.Where(stock => stock.BranchId == request.BranchId.Value);
        }

        if (request.BookId.HasValue)
        {
            stockQuery = stockQuery.Where(stock => stock.BookId == request.BookId.Value);
        }

        if (request.LowStockOnly is true)
        {
            stockQuery = stockQuery.Where(stock => stock.CurrentStock <= stock.MinStock);
        }

        var totals = await stockQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalBooksWithStock = group
                    .Where(stock => stock.CurrentStock > 0)
                    .Select(stock => stock.BookId)
                    .Distinct()
                    .Count(),
                TotalStockUnits = group.Sum(stock => stock.CurrentStock),
                LowStockItemsCount = group.Count(stock => stock.CurrentStock <= stock.MinStock),
                OutOfStockItemsCount = group.Count(stock => stock.CurrentStock == 0),
                BranchesWithStockCount = group
                    .Where(stock => stock.CurrentStock > 0)
                    .Select(stock => stock.BranchId)
                    .Distinct()
                    .Count(),
                InventoryValueEstimate = group.Sum(stock => stock.Book.Price * stock.CurrentStock),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var branchName = request.BranchId.HasValue
            ? await _context.Branches.AsNoTracking()
                .Where(branch => branch.Id == request.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var bookTitle = request.BookId.HasValue
            ? await _context.Books.AsNoTracking()
                .Where(book => book.Id == request.BookId.Value)
                .Select(book => book.Title)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (totals is null)
        {
            return new InventoryReportDto
            {
                BranchId = request.BranchId,
                BranchName = branchName,
                BookId = request.BookId,
                BookTitle = bookTitle,
                TotalBooksWithStock = 0,
                TotalStockUnits = 0,
                LowStockItemsCount = 0,
                OutOfStockItemsCount = 0,
                BranchesWithStockCount = 0,
                InventoryValueEstimate = 0,
            };
        }

        return new InventoryReportDto
        {
            BranchId = request.BranchId,
            BranchName = branchName,
            BookId = request.BookId,
            BookTitle = bookTitle,
            TotalBooksWithStock = totals.TotalBooksWithStock,
            TotalStockUnits = totals.TotalStockUnits,
            LowStockItemsCount = totals.LowStockItemsCount,
            OutOfStockItemsCount = totals.OutOfStockItemsCount,
            BranchesWithStockCount = totals.BranchesWithStockCount,
            InventoryValueEstimate = totals.InventoryValueEstimate,
        };
    }
}

