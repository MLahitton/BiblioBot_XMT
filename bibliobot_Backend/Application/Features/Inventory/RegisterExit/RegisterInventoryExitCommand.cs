using Application.Features.Inventory.Common;
using MediatR;

namespace Application.Features.Inventory.RegisterExit;

public sealed class RegisterInventoryExitCommand : IRequest<InventoryOperationResultDto>
{
    public Guid BookId { get; init; }
    public Guid BranchId { get; init; }
    public int Quantity { get; init; }
    public string? Reason { get; init; }
}

