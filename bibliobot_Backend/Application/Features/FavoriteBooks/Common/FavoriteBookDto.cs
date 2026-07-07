namespace Application.Features.FavoriteBooks.Common;

public sealed class FavoriteBookDto
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string? CoverUrl { get; init; }
    public DateTimeOffset AddedAtUtc { get; init; }
}
