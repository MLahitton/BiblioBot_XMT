using Application.Common.DTOs;

namespace Application.Features.Inventory.Common;

public sealed class InventoryMovementDto
{
    public Guid Id { get; init; }
    public Guid BookId { get; init; }
    public string BookTitle { get; init; } = string.Empty;
    public Guid BranchId { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string MovementTypeCode { get; init; } = string.Empty;
    public string MovementTypeName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int PreviousStock { get; init; }
    public int NewStock { get; init; }
    public string? Reason { get; init; }
    public Guid ActorId { get; init; }
    public string ActorName { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

