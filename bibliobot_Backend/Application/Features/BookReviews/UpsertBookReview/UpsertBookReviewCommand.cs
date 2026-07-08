using Application.Features.BookReviews.Common;
using MediatR;

namespace Application.Features.BookReviews.UpsertBookReview;

public sealed class UpsertBookReviewCommand : IRequest<BookReviewsSummaryDto>
{
    public Guid BookId { get; init; }
    public int Rating { get; init; }
    public string Comment { get; init; } = string.Empty;
}
