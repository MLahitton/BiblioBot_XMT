using Application.Features.Inventory.Common;
using MediatR;

namespace Application.Features.Inventory.RegisterEntry;

public sealed class RegisterInventoryEntryCommand : IRequest<InventoryOperationResultDto>
{
    public Guid BookId { get; init; }
    public Guid BranchId { get; init; }
    public int Quantity { get; init; }
    public string? Reason { get; init; }
    public int? MinStock { get; init; }
}

