namespace Application.Features.Books.Common;

public sealed class BookDetailDto
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
    public IReadOnlyCollection<string> Authors { get; init; } = [];
    public IReadOnlyCollection<string> Categories { get; init; } = [];
    public int TotalStock { get; init; }
    public IReadOnlyCollection<BookBranchStockDto> StockByBranch { get; init; } = [];
}
