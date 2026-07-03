using Application.Common.Interfaces;
using Application.Features.Inventory.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Inventory.RegisterAdjustment;

public sealed class RegisterInventoryAdjustmentCommandHandler : IRequestHandler<RegisterInventoryAdjustmentCommand, InventoryOperationResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RegisterInventoryAdjustmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InventoryOperationResultDto> Handle(
        RegisterInventoryAdjustmentCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var actorId = _currentUserService.UserId.Value;

        var book = await _context.Books.FirstOrDefaultAsync(
            existing => existing.Id == request.BookId && existing.IsActive && !existing.IsDeleted,
            cancellationToken);

        if (book is null)
        {
            throw new KeyNotFoundException("Libro no encontrado.");
        }

        var branch = await _context.Branches.FirstOrDefaultAsync(
            existing => existing.Id == request.BranchId && existing.IsActive,
            cancellationToken);

        if (branch is null)
        {
            throw new KeyNotFoundException("Sede no encontrada.");
        }

        if (request.NewStock < 0)
        {
            throw new ArgumentException("El stock nuevo debe ser mayor o igual a 0.");
        }

        var reason = request.Reason?.Trim();
        if (reason is not null && reason.Length > 250)
        {
            throw new ArgumentException("La razón debe tener máximo 250 caracteres.");
        }

        if (request.MinStock is < 0)
        {
            throw new ArgumentException("El stock mínimo debe ser mayor o igual a 0.");
        }

        var movementType = await _context.InventoryMovementTypes.FirstOrDefaultAsync(
            existing => existing.Code == InventoryMovementTypeCodes.Adjustment,
            cancellationToken);

        if (movementType is null)
        {
            throw new KeyNotFoundException("Tipo de movimiento no encontrado.");
        }

        var stock = await _context.InventoryStocks.FirstOrDefaultAsync(
            existing => existing.BookId == request.BookId && existing.BranchId == request.BranchId,
            cancellationToken);

        if (stock is null)
        {
            stock = new InventoryStock
            {
                BookId = request.BookId,
                BranchId = request.BranchId,
                CurrentStock = 0,
                MinStock = request.MinStock ?? 0,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            _context.InventoryStocks.Add(stock);
        }

        if (request.MinStock.HasValue)
        {
            stock.MinStock = request.MinStock.Value;
        }

        var previousStock = stock.CurrentStock;

        if (request.NewStock == previousStock)
        {
            if (request.MinStock is null)
            {
                return new InventoryOperationResultDto
                {
                    InventoryStockId = stock.Id,
                    BookId = stock.BookId,
                    BookTitle = book.Title,
                    BranchId = stock.BranchId,
                    BranchName = branch.Name,
                    PreviousStock = previousStock,
                    NewStock = previousStock,
                    MinStock = stock.MinStock,
                    MovementTypeCode = movementType.Code,
                    MovementId = null,
                    Reason = reason,
                };
            }

            stock.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return new InventoryOperationResultDto
            {
                InventoryStockId = stock.Id,
                BookId = stock.BookId,
                BookTitle = book.Title,
                BranchId = stock.BranchId,
                BranchName = branch.Name,
                PreviousStock = previousStock,
                NewStock = previousStock,
                MinStock = stock.MinStock,
                MovementTypeCode = movementType.Code,
                MovementId = null,
                Reason = reason,
            };
        }

        var newStock = request.NewStock;
        var movementQuantity = Math.Abs(newStock - previousStock);

        stock.CurrentStock = newStock;
        stock.UpdatedAt = DateTimeOffset.UtcNow;

        var movement = new InventoryMovement
        {
            BookId = request.BookId,
            BranchId = request.BranchId,
            MovementTypeId = movementType.Id,
            Quantity = movementQuantity,
            PreviousStock = previousStock,
            NewStock = newStock,
            Reason = reason,
            ActorId = actorId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _context.InventoryMovements.Add(movement);
        await _context.SaveChangesAsync(cancellationToken);

        return new InventoryOperationResultDto
        {
            InventoryStockId = stock.Id,
            BookId = stock.BookId,
            BookTitle = book.Title,
            BranchId = stock.BranchId,
            BranchName = branch.Name,
            PreviousStock = previousStock,
            NewStock = newStock,
            MinStock = stock.MinStock,
            MovementTypeCode = movementType.Code,
            MovementId = movement.Id,
            Reason = reason,
        };
    }
}

