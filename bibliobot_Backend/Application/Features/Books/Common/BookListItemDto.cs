namespace Application.Features.Books.Common;

public sealed class BookListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public string? PublisherName { get; init; }
    public decimal Price { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int PurchasedCount { get; init; }
    public int FavoriteCount { get; init; }
    public string? ImageUrl { get; init; }
    public IReadOnlyCollection<string> Authors { get; init; } = [];
    public IReadOnlyCollection<string> Categories { get; init; } = [];
    public int TotalStock { get; init; }
}
