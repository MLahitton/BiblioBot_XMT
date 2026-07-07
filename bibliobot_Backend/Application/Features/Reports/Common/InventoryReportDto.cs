namespace Application.Features.Reports.Common;

public sealed class InventoryReportDto
{
    public Guid? BranchId { get; init; }
    public string? BranchName { get; init; }
    public Guid? BookId { get; init; }
    public string? BookTitle { get; init; }
    public int TotalBooksWithStock { get; init; }
    public int TotalStockUnits { get; init; }
    public int LowStockItemsCount { get; init; }
    public int OutOfStockItemsCount { get; init; }
    public int BranchesWithStockCount { get; init; }
    public decimal InventoryValueEstimate { get; init; }
}

