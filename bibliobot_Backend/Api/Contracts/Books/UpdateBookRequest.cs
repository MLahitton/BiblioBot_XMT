namespace Api.Contracts.Books;

public sealed class UpdateBookRequest
{
    public string Title { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public string? Description { get; init; }
    public Guid? PublisherId { get; init; }
    public int? PublicationYear { get; init; }
    public string? Language { get; init; }
    public string? ImageUrl { get; init; }
    public decimal Price { get; init; }
    public IReadOnlyCollection<Guid> AuthorIds { get; init; } = [];
    public IReadOnlyCollection<Guid> CategoryIds { get; init; } = [];
}

