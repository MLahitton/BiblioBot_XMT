using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Text;
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
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var normalizedQuery = TextSearchNormalizer.Normalize(request.Query);

        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return new PagedResult<BookListItemDto>([], pageNumber, pageSize, 0);
        }

        var candidates = await _context.Books
            .AsNoTracking()
            .Where(book => book.IsActive && !book.IsDeleted)
            .Select(book => new BookListItemDto
            {
                Id = book.Id,
                Title = book.Title,
                Isbn = book.Isbn,
                PublisherName = book.Publisher != null ? book.Publisher.Name : null,
                Price = book.Price,
                AverageRating = book.BookReviews.Any() ? book.BookReviews.Average(review => review.Rating) : 0,
                ReviewCount = book.BookReviews.Count,
                PurchasedCount = book.SaleDetails.Sum(saleDetail => saleDetail.Quantity),
                FavoriteCount = book.UserFavoriteBooks.Count,
                ImageUrl = book.ImageUrl,
                Authors = book.BookAuthors
                    .Select(bookAuthor => bookAuthor.Author.FullName)
                    .OrderBy(authorName => authorName)
                    .ToList(),
                Categories = book.BookCategories
                    .Select(bookCategory => bookCategory.Category.Name)
                    .OrderBy(categoryName => categoryName)
                    .ToList(),
                TotalStock = book.InventoryStocks.Sum(stock => stock.CurrentStock)
            })
            .ToListAsync(cancellationToken);

        var filteredBooks = candidates
            .Where(book => MatchesBook(book, normalizedQuery))
            .OrderBy(book => book.Title)
            .ToList();

        var totalCount = filteredBooks.Count;
        var items = filteredBooks
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<BookListItemDto>(items, pageNumber, pageSize, totalCount);
    }

    private static bool MatchesBook(BookListItemDto book, string normalizedQuery)
    {
        return TextSearchNormalizer.ContainsNormalized(book.Title, normalizedQuery)
            || TextSearchNormalizer.ContainsNormalized(book.Isbn, normalizedQuery)
            || TextSearchNormalizer.ContainsNormalized(book.PublisherName, normalizedQuery)
            || book.Authors.Any(authorName => TextSearchNormalizer.ContainsNormalized(authorName, normalizedQuery))
            || book.Categories.Any(categoryName => TextSearchNormalizer.ContainsNormalized(categoryName, normalizedQuery));
    }
}
