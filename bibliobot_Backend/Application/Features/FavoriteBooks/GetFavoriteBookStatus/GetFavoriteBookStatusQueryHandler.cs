using Application.Common.Interfaces;
using Application.Features.FavoriteBooks.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FavoriteBooks.GetFavoriteBookStatus;

public sealed class GetFavoriteBookStatusQueryHandler : IRequestHandler<GetFavoriteBookStatusQuery, FavoriteBookStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetFavoriteBookStatusQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<FavoriteBookStatusDto> Handle(
        GetFavoriteBookStatusQuery request,
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

        var bookExists = await _context.Books
            .AsNoTracking()
            .AnyAsync(
                book => book.Id == request.BookId && book.IsActive && !book.IsDeleted,
                cancellationToken);

        if (!bookExists)
        {
            throw new KeyNotFoundException("El libro seleccionado no existe.");
        }

        var isFavorite = await _context.UserFavoriteBooks
            .AnyAsync(
                favorite => favorite.UserId == actorId && favorite.BookId == request.BookId,
                cancellationToken);

        return new FavoriteBookStatusDto
        {
            BookId = request.BookId,
            IsFavorite = isFavorite,
        };
    }
}
