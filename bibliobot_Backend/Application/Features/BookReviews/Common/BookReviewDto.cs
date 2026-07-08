namespace Application.Features.BookReviews.Common;

public sealed class BookReviewDto
{
    public Guid Id { get; init; }
    public Guid BookId { get; init; }
    public Guid UserId { get; init; }
    public string UserFullName { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
    public bool IsVerifiedPurchase { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
