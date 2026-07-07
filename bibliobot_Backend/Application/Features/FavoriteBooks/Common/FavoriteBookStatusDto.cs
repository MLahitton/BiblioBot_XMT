namespace Application.Features.FavoriteBooks.Common;

public sealed class FavoriteBookStatusDto
{
    public Guid BookId { get; init; }
    public bool IsFavorite { get; init; }
}
