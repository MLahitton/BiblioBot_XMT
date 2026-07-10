using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Text;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchBooks;

public sealed class SearchBooksLookupQueryHandler : IRequestHandler<SearchBooksLookupQuery, PagedResult<LookupBookDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchBooksLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupBookDto>> Handle(SearchBooksLookupQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var normalizedQuery = TextSearchNormalizer.Normalize(request.Q);
        var normalizedIsbn = TextSearchNormalizer.Normalize(request.Isbn);

        var candidates = await _context.Books
            .AsNoTracking()
            .Where(book => book.IsActive && !book.IsDeleted)
            .Select(book => new BookLookupSearchCandidate(
                new LookupBookDto
                {
                    Id = book.Id,
                    Label = book.Isbn == null ? book.Title : book.Title + " (" + book.Isbn + ")",
                    Title = book.Title,
                    Isbn = book.Isbn,
                    Price = book.Price,
                    PublisherName = book.Publisher != null ? book.Publisher.Name : null,
                    Authors = book.BookAuthors
                        .Select(bookAuthor => bookAuthor.Author.FullName)
                        .OrderBy(authorName => authorName)
                        .ToList(),
                    Categories = book.BookCategories
                        .Select(bookCategory => bookCategory.Category.Name)
                        .OrderBy(categoryName => categoryName)
                        .ToList(),
                    TotalStock = book.InventoryStocks.Sum(stock => stock.CurrentStock),
                    IsActive = book.IsActive
                },
                book.Description))
            .ToListAsync(cancellationToken);

        IEnumerable<BookLookupSearchCandidate> filteredCandidates = candidates;

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            filteredCandidates = filteredCandidates.Where(candidate => MatchesBook(candidate, normalizedQuery));
        }

        if (!string.IsNullOrWhiteSpace(normalizedIsbn))
        {
            filteredCandidates = filteredCandidates.Where(candidate =>
                TextSearchNormalizer.ContainsNormalized(candidate.Item.Isbn, normalizedIsbn));
        }

        var filteredItems = filteredCandidates
            .OrderBy(candidate => candidate.Item.Title)
            .Select(candidate => candidate.Item)
            .ToList();

        var totalCount = filteredItems.Count;
        var items = filteredItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<LookupBookDto>(items, pageNumber, pageSize, totalCount);
    }

    private static bool MatchesBook(BookLookupSearchCandidate candidate, string normalizedQuery)
    {
        return TextSearchNormalizer.ContainsNormalized(candidate.Item.Title, normalizedQuery)
            || TextSearchNormalizer.ContainsNormalized(candidate.Item.Isbn, normalizedQuery)
            || TextSearchNormalizer.ContainsNormalized(candidate.Description, normalizedQuery)
            || TextSearchNormalizer.ContainsNormalized(candidate.Item.PublisherName, normalizedQuery)
            || candidate.Item.Authors.Any(authorName => TextSearchNormalizer.ContainsNormalized(authorName, normalizedQuery))
            || candidate.Item.Categories.Any(categoryName => TextSearchNormalizer.ContainsNormalized(categoryName, normalizedQuery));
    }

    private sealed record BookLookupSearchCandidate(LookupBookDto Item, string? Description);
}
