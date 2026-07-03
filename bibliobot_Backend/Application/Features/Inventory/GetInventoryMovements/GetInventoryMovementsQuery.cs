using Application.Common.DTOs;
using Application.Features.Inventory.Common;
using MediatR;

namespace Application.Features.Inventory.GetInventoryMovements;

public sealed class GetInventoryMovementsQuery : IRequest<PagedResult<InventoryMovementDto>>
{
    public Guid? BookId { get; init; }
    public Guid? BranchId { get; init; }
    public string? MovementTypeCode { get; init; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

