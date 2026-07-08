using Application.Features.BookReviews.Common;
using MediatR;

namespace Application.Features.BookReviews.GetBookReviews;

public sealed class GetBookReviewsQuery : IRequest<BookReviewsSummaryDto>
{
    public Guid BookId { get; init; }
}
