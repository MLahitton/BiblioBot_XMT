namespace Application.Features.Sales.Common;

public sealed class SaleDetailDto
{
    public Guid Id { get; init; }
    public Guid BookId { get; init; }
    public string BookTitleSnapshot { get; init; } = string.Empty;
    public string? IsbnSnapshot { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

