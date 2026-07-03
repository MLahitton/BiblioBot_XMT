using Application.Common.Interfaces;
using Application.Features.Inventory.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Inventory.RegisterExit;

public sealed class RegisterInventoryExitCommandHandler : IRequestHandler<RegisterInventoryExitCommand, InventoryOperationResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RegisterInventoryExitCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InventoryOperationResultDto> Handle(
        RegisterInventoryExitCommand request,
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

        if (request.Quantity <= 0)
        {
            throw new ArgumentException("La cantidad debe ser mayor a 0.");
        }

        var reason = request.Reason?.Trim();
        if (reason is not null && reason.Length > 250)
        {
            throw new ArgumentException("La razón debe tener máximo 250 caracteres.");
        }

        var stock = await _context.InventoryStocks.FirstOrDefaultAsync(
            existing => existing.BookId == request.BookId && existing.BranchId == request.BranchId,
            cancellationToken);

        if (stock is null)
        {
            throw new KeyNotFoundException("Stock no encontrado.");
        }

        if (stock.CurrentStock < request.Quantity)
        {
            throw new InvalidOperationException("Stock insuficiente.");
        }

        var movementType = await _context.InventoryMovementTypes.FirstOrDefaultAsync(
            existing => existing.Code == InventoryMovementTypeCodes.Exit,
            cancellationToken);

        if (movementType is null)
        {
            throw new KeyNotFoundException("Tipo de movimiento no encontrado.");
        }

        var previousStock = stock.CurrentStock;
        var newStock = previousStock - request.Quantity;

        stock.CurrentStock = newStock;
        stock.UpdatedAt = DateTimeOffset.UtcNow;

        var movement = new InventoryMovement
        {
            BookId = request.BookId,
            BranchId = request.BranchId,
            MovementTypeId = movementType.Id,
            Quantity = request.Quantity,
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

