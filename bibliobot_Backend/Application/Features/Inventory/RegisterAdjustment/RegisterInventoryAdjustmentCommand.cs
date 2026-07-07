using Application.Features.Inventory.Common;
using MediatR;

namespace Application.Features.Inventory.RegisterAdjustment;

public sealed class RegisterInventoryAdjustmentCommand : IRequest<InventoryOperationResultDto>
{
    public Guid BookId { get; init; }
    public Guid BranchId { get; init; }
    public int NewStock { get; init; }
    public string? Reason { get; init; }
    public int? MinStock { get; init; }
}

