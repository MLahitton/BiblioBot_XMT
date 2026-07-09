using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.GetAdminProducts;

public sealed class GetAdminProductsQueryHandler : IRequestHandler<GetAdminProductsQuery, PagedResult<AdminProductDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AdminProductDto>> Handle(
        GetAdminProductsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _context.Books.AsNoTracking()
            .Where(book => !book.IsDeleted);

        var search = request.Search?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var loweredSearch = search.ToLowerInvariant();
            query = query.Where(book =>
                book.Id.ToString().ToLower().Contains(loweredSearch)
                || book.Title.ToLower().Contains(loweredSearch)
                || (book.Isbn != null && book.Isbn.ToLower().Contains(loweredSearch))
                || book.BookAuthors.Any(author => author.Author.FullName.ToLower().Contains(loweredSearch)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(book => book.IsActive == request.IsActive.Value);
        }

        query = (request.SortBy ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "author" => query
                .OrderBy(book => book.BookAuthors
                    .Select(author => author.Author.FullName)
                    .FirstOrDefault())
                .ThenBy(book => book.Title),
            "price_desc" => query.OrderByDescending(book => book.Price).ThenBy(book => book.Title),
            "price_asc" => query.OrderBy(book => book.Price).ThenBy(book => book.Title),
            "purchased_desc" => query
                .OrderByDescending(book => book.SaleDetails.Sum(detail => detail.Quantity))
                .ThenBy(book => book.Title),
            "favorites_desc" => query
                .OrderByDescending(book => book.UserFavoriteBooks.Count)
                .ThenBy(book => book.Title),
            _ => query.OrderBy(book => book.Title),
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var books = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(book => book.Publisher)
            .Include(book => book.BookAuthors)
                .ThenInclude(author => author.Author)
            .Include(book => book.BookCategories)
                .ThenInclude(category => category.Category)
            .Include(book => book.InventoryStocks)
                .ThenInclude(stock => stock.Branch)
            .Include(book => book.SaleDetails)
            .Include(book => book.UserFavoriteBooks)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminProductDto>(
            books.Select(AdminProductMapping.ToDto).ToList(),
            pageNumber,
            pageSize,
            totalCount);
    }
}
