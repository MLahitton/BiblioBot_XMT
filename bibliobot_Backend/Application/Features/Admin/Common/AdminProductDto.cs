namespace Application.Features.Admin.Common;

public sealed class AdminProductDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public string? Description { get; init; }
    public string? PublisherName { get; init; }
    public int? PublicationYear { get; init; }
    public string? Language { get; init; }
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyCollection<string> Authors { get; init; } = [];
    public IReadOnlyCollection<string> Categories { get; init; } = [];
    public Guid? BranchId { get; init; }
    public string? BranchName { get; init; }
    public int CurrentStock { get; init; }
    public int MinStock { get; init; }
    public int PurchasedCount { get; init; }
    public int FavoriteCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
