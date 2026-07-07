using Application.Common.DTOs;
using Application.Features.Inventory.Common;
using MediatR;

namespace Application.Features.Inventory.GetInventory;

public sealed class GetInventoryQuery : IRequest<PagedResult<InventoryStockDto>>
{
    public Guid? BookId { get; init; }
    public Guid? BranchId { get; init; }
    public bool LowStockOnly { get; init; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

