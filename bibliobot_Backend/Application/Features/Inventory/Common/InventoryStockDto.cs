using Application.Common.DTOs;

namespace Application.Features.Inventory.Common;

public sealed class InventoryStockDto
{
    public Guid InventoryStockId { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int MinStock { get; init; }
    public bool IsLowStock { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

