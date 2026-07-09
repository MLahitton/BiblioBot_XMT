namespace Application.Features.BookReviews.Common;

public sealed class BookReviewsSummaryDto
{
    public Guid BookId { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public IReadOnlyCollection<BookReviewDto> Items { get; init; } = [];
}
