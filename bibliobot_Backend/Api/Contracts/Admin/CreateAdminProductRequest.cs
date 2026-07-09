namespace Api.Contracts.Admin;

public sealed class CreateAdminProductRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public string? Description { get; init; }
    public string? PublisherName { get; init; }
    public int? PublicationYear { get; init; }
    public string? Language { get; init; }
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
    public IReadOnlyCollection<string> AuthorNames { get; init; } = [];
    public IReadOnlyCollection<string> CategoryNames { get; init; } = [];
    public Guid? BranchId { get; init; }
    public int CurrentStock { get; init; }
    public int MinStock { get; init; }
}
