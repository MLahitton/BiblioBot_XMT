using Application.Common.Interfaces;
using Application.Features.FavoriteBooks.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.FavoriteBooks.AddFavoriteBook;

public sealed class AddFavoriteBookCommandHandler : IRequestHandler<AddFavoriteBookCommand, FavoriteBookDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddFavoriteBookCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<FavoriteBookDto> Handle(
        AddFavoriteBookCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var actorId = _currentUserService.UserId.Value;
        var actor = await _context.Users.FirstOrDefaultAsync(
            user => user.Id == actorId,
            cancellationToken);

        if (actor is null || !actor.IsActive || actor.IsDeleted)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var book = await _context.Books.AsNoTracking()
            .Include(book => book.BookAuthors)
                .ThenInclude(author => author.Author)
            .FirstOrDefaultAsync(
                book => book.Id == request.BookId && book.IsActive && !book.IsDeleted,
                cancellationToken);

        if (book is null)
        {
            throw new KeyNotFoundException("El libro seleccionado no existe.");
        }

        var existing = await _context.UserFavoriteBooks
            .FirstOrDefaultAsync(
                favorite => favorite.UserId == actorId && favorite.BookId == request.BookId,
                cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException("El libro ya está en favoritos.");
        }

        var favorite = new UserFavoriteBook
        {
            UserId = actorId,
            BookId = request.BookId,
        };

        _context.UserFavoriteBooks.Add(favorite);
        await _context.SaveChangesAsync(cancellationToken);

        return new FavoriteBookDto
        {
            BookId = book.Id,
            Title = book.Title,
            Author = book.BookAuthors
                .OrderBy(author => author.Author.FullName)
                .Select(author => author.Author.FullName)
                .FirstOrDefault() ?? string.Empty,
            CoverUrl = book.ImageUrl,
            AddedAtUtc = favorite.CreatedAt,
        };
    }
}
