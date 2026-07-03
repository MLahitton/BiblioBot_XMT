using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Inventory.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Inventory.GetInventory;

public sealed class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, PagedResult<InventoryStockDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<InventoryStockDto>> Handle(
        GetInventoryQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _context.InventoryStocks.AsNoTracking()
            .Where(stock => !stock.Book.IsDeleted);

        if (request.BookId.HasValue)
        {
            query = query.Where(stock => stock.BookId == request.BookId.Value);
        }

        if (request.BranchId.HasValue)
        {
            query = query.Where(stock => stock.BranchId == request.BranchId.Value);
        }

        if (request.LowStockOnly)
        {
            query = query.Where(stock => stock.CurrentStock <= stock.MinStock);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(stock => stock.Book.Title)
            .ThenBy(stock => stock.Branch.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(stock => new InventoryStockDto
            {
                InventoryStockId = stock.Id,
                BookId = stock.BookId,
                BookTitle = stock.Book.Title,
                Isbn = stock.Book.Isbn,
                BranchId = stock.BranchId,
                BranchName = stock.Branch.Name,
                CurrentStock = stock.CurrentStock,
                MinStock = stock.MinStock,
                IsLowStock = stock.CurrentStock <= stock.MinStock,
                UpdatedAt = stock.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryStockDto>(items, pageNumber, pageSize, totalCount);
    }
}

