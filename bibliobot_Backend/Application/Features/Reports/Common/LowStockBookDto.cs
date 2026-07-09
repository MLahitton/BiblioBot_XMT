namespace Application.Features.Reports.Common;

public sealed class LowStockBookDto
{
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int MinimumStock { get; init; }
    public int Difference { get; init; }
}

