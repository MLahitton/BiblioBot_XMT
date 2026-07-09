using Application.Common.Interfaces;
using Application.Features.FavoriteBooks.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FavoriteBooks.ListFavoriteBooks;

public sealed class ListFavoriteBooksQueryHandler : IRequestHandler<ListFavoriteBooksQuery, IReadOnlyCollection<FavoriteBookDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ListFavoriteBooksQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyCollection<FavoriteBookDto>> Handle(
        ListFavoriteBooksQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var actorId = _currentUserService.UserId.Value;
        var actor = await _context.Users
            .FirstOrDefaultAsync(user => user.Id == actorId, cancellationToken);

        if (actor is null || !actor.IsActive || actor.IsDeleted)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        return await _context.UserFavoriteBooks
            .AsNoTracking()
            .Where(favorite => favorite.UserId == actorId && favorite.Book.IsActive && !favorite.Book.IsDeleted)
            .OrderByDescending(favorite => favorite.CreatedAt)
            .Select(favorite => new FavoriteBookDto
            {
                BookId = favorite.BookId,
                Title = favorite.Book.Title,
                Author = favorite.Book.BookAuthors
                    .Select(author => author.Author.FullName)
                    .OrderBy(authorName => authorName)
                    .FirstOrDefault() ?? string.Empty,
                CoverUrl = favorite.Book.ImageUrl,
                AddedAtUtc = favorite.CreatedAt,
            })
            .ToListAsync(cancellationToken);
    }
}
