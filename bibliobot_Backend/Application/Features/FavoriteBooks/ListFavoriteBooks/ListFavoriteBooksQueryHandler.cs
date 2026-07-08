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
        var actorId = await GetActiveActorId(cancellationToken);

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
                Category = favorite.Book.BookCategories
                    .Select(category => category.Category.Name)
                    .OrderBy(categoryName => categoryName)
                    .FirstOrDefault() ?? string.Empty,
                Description = favorite.Book.Description,
                CoverUrl = favorite.Book.ImageUrl,
                Price = favorite.Book.Price,
                TotalStock = favorite.Book.InventoryStocks.Sum(stock => stock.CurrentStock),
                AddedAtUtc = favorite.CreatedAt,
            })
            .ToListAsync(cancellationToken);
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
