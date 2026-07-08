using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FavoriteBooks.RemoveFavoriteBook;

public sealed class RemoveFavoriteBookCommandHandler : IRequestHandler<RemoveFavoriteBookCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RemoveFavoriteBookCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<bool> Handle(RemoveFavoriteBookCommand request, CancellationToken cancellationToken)
    {
        var actorId = await GetActiveActorId(cancellationToken);

        var bookExists = await _context.Books
            .AsNoTracking()
            .AnyAsync(
                book => book.Id == request.BookId && book.IsActive && !book.IsDeleted,
                cancellationToken);

        if (!bookExists)
        {
            throw new KeyNotFoundException("El libro seleccionado no existe.");
        }

        var favorite = await _context.UserFavoriteBooks.FirstOrDefaultAsync(
            favorite => favorite.UserId == actorId && favorite.BookId == request.BookId,
            cancellationToken);

        if (favorite is null)
        {
            throw new KeyNotFoundException("El libro no esta en favoritos.");
        }

        _context.UserFavoriteBooks.Remove(favorite);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
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
}
