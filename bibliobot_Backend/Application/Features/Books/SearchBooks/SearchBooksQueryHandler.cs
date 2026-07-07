using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Books.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Books.SearchBooks;

public sealed class SearchBooksQueryHandler : IRequestHandler<SearchBooksQuery, PagedResult<BookListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchBooksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<BookListItemDto>> Handle(SearchBooksQuery request, CancellationToken cancellationToken)
    {
        var normalizedQuery = request.Query.Trim();
        if (normalizedQuery.Length < 2)
        {
            throw new ArgumentException("El criterio de búsqueda debe tener al menos 2 caracteres.");
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var normalizedQueryLower = normalizedQuery.ToLower();

        var query = _context.Books.AsNoTracking()
            .Where(book => book.IsActive && !book.IsDeleted)
            .Where(book =>
                book.Title.ToLower().Contains(normalizedQueryLower) ||
                (book.Isbn != null && book.Isbn.ToLower().Contains(normalizedQueryLower)) ||
                book.BookAuthors.Any(author => author.Author.FullName.ToLower().Contains(normalizedQueryLower)) ||
                book.BookCategories.Any(category => category.Category.Name.ToLower().Contains(normalizedQueryLower)) ||
                (book.Publisher != null && book.Publisher.Name.ToLower().Contains(normalizedQueryLower))
            );

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(book => book.Title)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(book => new BookListItemDto
            {
                Id = book.Id,
                Title = book.Title,
                Isbn = book.Isbn,
                PublisherName = book.Publisher != null ? book.Publisher.Name : null,
                Price = book.Price,
                ImageUrl = book.ImageUrl,
                Authors = book.BookAuthors.Select(author => author.Author.FullName).Distinct().ToList(),
                Categories = book.BookCategories.Select(category => category.Category.Name).Distinct().ToList(),
                TotalStock = book.InventoryStocks.Sum(stock => stock.CurrentStock),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BookListItemDto>(items, pageNumber, pageSize, totalCount);
    }
}
