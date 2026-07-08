namespace Application.Features.FavoriteBooks.Common;

public sealed class FavoriteBookDto
{
    public Guid BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? CoverUrl { get; init; }
    public decimal Price { get; init; }
    public int TotalStock { get; init; }
    public DateTimeOffset AddedAtUtc { get; init; }
}
