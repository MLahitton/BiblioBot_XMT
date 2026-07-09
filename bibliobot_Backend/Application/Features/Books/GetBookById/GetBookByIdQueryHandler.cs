using Application.Common.Interfaces;
using Application.Features.Books.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Books.GetBookById;

public sealed class GetBookByIdQueryHandler : IRequestHandler<GetBookByIdQuery, BookDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetBookByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BookDetailDto?> Handle(
        GetBookByIdQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _context.Books.AsNoTracking()
            .Where(book => book.Id == request.Id && book.IsActive && !book.IsDeleted)
            .Select(book => new BookDetailDto
            {
                Id = book.Id,
                Title = book.Title,
                Isbn = book.Isbn,
                Description = book.Description,
                PublisherName = book.Publisher != null ? book.Publisher.Name : null,
                PublicationYear = book.PublicationYear,
                Language = book.Language,
                ImageUrl = book.ImageUrl,
                Price = book.Price,
                AverageRating = book.BookReviews.Any(review => review.User.IsActive && !review.User.IsDeleted)
                    ? book.BookReviews
                        .Where(review => review.User.IsActive && !review.User.IsDeleted)
                        .Average(review => review.Rating)
                    : 0,
                ReviewCount = book.BookReviews.Count(review => review.User.IsActive && !review.User.IsDeleted),
                Authors = book.BookAuthors.Select(author => author.Author.FullName).Distinct().ToList(),
                Categories = book.BookCategories.Select(category => category.Category.Name).Distinct().ToList(),
                TotalStock = book.InventoryStocks.Sum(stock => stock.CurrentStock),
                StockByBranch = book.InventoryStocks.Select(stock => new BookBranchStockDto
                {
                    BranchId = stock.BranchId,
                    BranchName = stock.Branch.Name,
                    CurrentStock = stock.CurrentStock,
                }).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }
}
