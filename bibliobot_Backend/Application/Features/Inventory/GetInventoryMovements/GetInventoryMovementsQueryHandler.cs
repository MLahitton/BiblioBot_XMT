using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Inventory.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Inventory.GetInventoryMovements;

public sealed class GetInventoryMovementsQueryHandler : IRequestHandler<
    GetInventoryMovementsQuery,
    PagedResult<InventoryMovementDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInventoryMovementsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<InventoryMovementDto>> Handle(
        GetInventoryMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _context.InventoryMovements.AsNoTracking();

        if (request.BookId.HasValue)
        {
            query = query.Where(movement => movement.BookId == request.BookId.Value);
        }

        if (request.BranchId.HasValue)
        {
            query = query.Where(movement => movement.BranchId == request.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.MovementTypeCode))
        {
            var movementTypeCode = request.MovementTypeCode.Trim().ToUpperInvariant();

            query = query.Where(movement => movement.MovementType.Code.ToUpper() == movementTypeCode);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(movement => movement.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(movement => new InventoryMovementDto
            {
                Id = movement.Id,
                BookId = movement.BookId,
                BookTitle = movement.Book.Title,
                BranchId = movement.BranchId,
                BranchName = movement.Branch.Name,
                MovementTypeCode = movement.MovementType.Code,
                MovementTypeName = movement.MovementType.Name,
                Quantity = movement.Quantity,
                PreviousStock = movement.PreviousStock,
                NewStock = movement.NewStock,
                Reason = movement.Reason,
                ActorId = movement.ActorId,
                ActorName = movement.Actor.FullName,
                CreatedAt = movement.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryMovementDto>(items, pageNumber, pageSize, totalCount);
    }
}

