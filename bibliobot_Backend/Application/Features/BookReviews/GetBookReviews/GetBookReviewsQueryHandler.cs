using Application.Common.Interfaces;
using Application.Features.BookReviews.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BookReviews.GetBookReviews;

public sealed class GetBookReviewsQueryHandler : IRequestHandler<GetBookReviewsQuery, BookReviewsSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetBookReviewsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BookReviewsSummaryDto> Handle(
        GetBookReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var bookExists = await _context.Books.AsNoTracking()
            .AnyAsync(book => book.Id == request.BookId && book.IsActive && !book.IsDeleted, cancellationToken);

        if (!bookExists)
        {
            throw new KeyNotFoundException("Libro no encontrado.");
        }

        var reviews = await _context.BookReviews.AsNoTracking()
            .Where(review => review.BookId == request.BookId && review.User.IsActive && !review.User.IsDeleted)
            .OrderByDescending(review => review.UpdatedAt ?? review.CreatedAt)
            .Select(review => new BookReviewDto
            {
                Id = review.Id,
                BookId = review.BookId,
                UserId = review.UserId,
                UserFullName = review.User.FullName,
                Rating = review.Rating,
                Comment = review.Comment,
                IsVerifiedPurchase = review.IsVerifiedPurchase,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return new BookReviewsSummaryDto
        {
            BookId = request.BookId,
            AverageRating = reviews.Count > 0
                ? Math.Round(reviews.Average(review => review.Rating), 1)
                : 0,
            ReviewCount = reviews.Count,
            Items = reviews,
        };
    }
}
