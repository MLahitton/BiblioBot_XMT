using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Books.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Books.GetBooks;

public sealed class GetBooksQueryHandler : IRequestHandler<GetBooksQuery, PagedResult<BookListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBooksQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<BookListItemDto>> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _context.Books.AsNoTracking()
            .Where(book => book.IsActive && !book.IsDeleted);

        if (request.CategoryId is Guid categoryId)
        {
            query = query.Where(book => book.BookCategories.Any(category => category.CategoryId == categoryId));
        }

        if (request.AuthorId is Guid authorId)
        {
            query = query.Where(book => book.BookAuthors.Any(author => author.AuthorId == authorId));
        }

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

        return new PagedResult<BookListItemDto>(
            items,
            pageNumber,
            pageSize,
            totalCount);
    }
}
