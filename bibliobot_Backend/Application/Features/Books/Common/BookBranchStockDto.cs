namespace Application.Features.Books.Common;

public sealed class BookBranchStockDto
{
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
}
