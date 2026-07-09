using System;
using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchBooks;

public sealed class SearchBooksLookupQueryHandler
    : IRequestHandler<SearchBooksLookupQuery, PagedResult<LookupBookDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchBooksLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupBookDto>> Handle(
        SearchBooksLookupQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.Books.AsNoTracking()
            .Where(book => !book.IsDeleted && book.IsActive);

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            query = query.Where(book =>
                book.Title.ToUpper().Contains(normalized) ||
                (book.Isbn != null && book.Isbn.ToUpper().Contains(normalized)) ||
                (book.Description != null && book.Description.ToUpper().Contains(normalized)));
        }

        var isbn = request.Isbn?.Trim();
        if (!string.IsNullOrWhiteSpace(isbn))
        {
            var normalizedIsbn = isbn!.ToUpperInvariant();
            query = query.Where(book =>
                book.Isbn != null && book.Isbn.ToUpper().Contains(normalizedIsbn));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(book => book.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(book => new LookupBookDto
            {
                Id = book.Id,
                Label = BuildLabel(book.Title, book.Isbn),
                Title = book.Title,
                Isbn = book.Isbn,
                Price = book.Price,
                PublisherName = book.Publisher != null ? book.Publisher.Name : null,
                Authors = book.BookAuthors
                    .Select(author => author.Author.FullName)
                    .Distinct()
                    .ToList(),
                Categories = book.BookCategories
                    .Select(category => category.Category.Name)
                    .Distinct()
                    .ToList(),
                TotalStock = book.InventoryStocks.Sum(stock => stock.CurrentStock),
                IsActive = book.IsActive,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupBookDto>(items, pageNumber, pageSize, totalCount);
    }

    private static string BuildLabel(string title, string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return title;
        }

        return $"{title} - ISBN {isbn}";
    }
}

