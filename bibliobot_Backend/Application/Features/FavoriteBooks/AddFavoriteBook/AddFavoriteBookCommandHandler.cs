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
        var actorId = await GetActiveActorId(cancellationToken);

        var book = await _context.Books
            .AsNoTracking()
            .Where(book => book.Id == request.BookId && book.IsActive && !book.IsDeleted)
            .Select(book => new
            {
                book.Id,
                book.Title,
                book.Description,
                book.ImageUrl,
                book.Price,
                Author = book.BookAuthors
                    .Select(author => author.Author.FullName)
                    .OrderBy(authorName => authorName)
                    .FirstOrDefault(),
                Category = book.BookCategories
                    .Select(category => category.Category.Name)
                    .OrderBy(categoryName => categoryName)
                    .FirstOrDefault(),
                TotalStock = book.InventoryStocks.Sum(stock => stock.CurrentStock),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (book is null)
        {
            throw new KeyNotFoundException("El libro seleccionado no existe.");
        }

        var exists = await _context.UserFavoriteBooks.AnyAsync(
            favorite => favorite.UserId == actorId && favorite.BookId == request.BookId,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("El libro ya esta en favoritos.");
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
            Author = book.Author ?? string.Empty,
            Category = book.Category ?? string.Empty,
            Description = book.Description,
            CoverUrl = book.ImageUrl,
            Price = book.Price,
            TotalStock = book.TotalStock,
            AddedAtUtc = favorite.CreatedAt,
        };
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
