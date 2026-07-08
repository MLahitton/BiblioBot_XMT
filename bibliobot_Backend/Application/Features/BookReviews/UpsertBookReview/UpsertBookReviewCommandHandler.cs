using Application.Common.Interfaces;
using Application.Features.BookReviews.Common;
using Application.Features.BookReviews.GetBookReviews;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.BookReviews.UpsertBookReview;

public sealed class UpsertBookReviewCommandHandler : IRequestHandler<UpsertBookReviewCommand, BookReviewsSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public UpsertBookReviewCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<BookReviewsSummaryDto> Handle(
        UpsertBookReviewCommand request,
        CancellationToken cancellationToken)
    {
        var actorId = await GetActiveActorId(cancellationToken);
        var comment = request.Comment?.Trim() ?? string.Empty;

        if (request.Rating is < 1 or > 5)
        {
            throw new ArgumentException("La calificacion debe estar entre 1 y 5.");
        }

        if (comment.Length < 5 || comment.Length > 1000)
        {
            throw new ArgumentException("La resena debe tener entre 5 y 1000 caracteres.");
        }

        var bookExists = await _context.Books.AnyAsync(
            book => book.Id == request.BookId && book.IsActive && !book.IsDeleted,
            cancellationToken);

        if (!bookExists)
        {
            throw new KeyNotFoundException("Libro no encontrado.");
        }

        var review = await _context.BookReviews.FirstOrDefaultAsync(
            existing => existing.BookId == request.BookId && existing.UserId == actorId,
            cancellationToken);

        var isVerifiedPurchase = await HasVerifiedPurchaseAsync(actorId, request.BookId, cancellationToken);

        if (review is null)
        {
            review = new BookReview
            {
                BookId = request.BookId,
                UserId = actorId,
                Rating = request.Rating,
                Comment = comment,
                IsVerifiedPurchase = isVerifiedPurchase,
            };

            _context.BookReviews.Add(review);
        }
        else
        {
            review.Rating = request.Rating;
            review.Comment = comment;
            review.IsVerifiedPurchase = isVerifiedPurchase;
            review.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _sender.Send(
            new GetBookReviewsQuery { BookId = request.BookId },
            cancellationToken);
    }

    private async Task<Guid> GetActiveActorId(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var actorId = _currentUserService.UserId.Value;
        var isActiveActor = await _context.Users.AnyAsync(
            user => user.Id == actorId && user.IsActive && !user.IsDeleted,
            cancellationToken);

        if (!isActiveActor)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        return actorId;
    }

    private async Task<bool> HasVerifiedPurchaseAsync(
        Guid userId,
        Guid bookId,
        CancellationToken cancellationToken)
    {
        return await _context.SaleDetails.AsNoTracking()
            .AnyAsync(
                detail => detail.BookId == bookId &&
                    detail.Sale.CustomerId == userId &&
                    detail.Sale.ConfirmedAt != null,
                cancellationToken);
    }
}
