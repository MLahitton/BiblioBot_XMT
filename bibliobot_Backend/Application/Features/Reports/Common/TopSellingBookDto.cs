namespace Application.Features.Reports.Common;

public sealed class TopSellingBookDto
{
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public int UnitsSold { get; init; }
    public int SalesCount { get; init; }
    public decimal Revenue { get; init; }
}

