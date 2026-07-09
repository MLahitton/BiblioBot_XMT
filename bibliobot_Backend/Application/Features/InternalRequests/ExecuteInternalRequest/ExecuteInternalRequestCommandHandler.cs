using Application.Common.Interfaces;
using Application.Features.InternalRequests.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InternalRequests.ExecuteInternalRequest;

public sealed class ExecuteInternalRequestCommandHandler : IRequestHandler<ExecuteInternalRequestCommand, InternalRequestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ExecuteInternalRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InternalRequestDto> Handle(
        ExecuteInternalRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var actorId = _currentUserService.UserId.Value;

        var requestEntity = await _context.InternalRequests
            .Include(internalRequest => internalRequest.RequestType)
            .Include(internalRequest => internalRequest.Status)
            .Include(internalRequest => internalRequest.Actor)
            .Include(internalRequest => internalRequest.SourceBranch)
            .Include(internalRequest => internalRequest.TargetBranch)
            .Include(internalRequest => internalRequest.Items)
                .ThenInclude(item => item.Book)
            .FirstOrDefaultAsync(internalRequest => internalRequest.Id == request.Id, cancellationToken);

        if (requestEntity is null)
        {
            throw new KeyNotFoundException("Solicitud no encontrada.");
        }

        var currentStatus = requestEntity.Status?.Code ?? string.Empty;
        if (currentStatus == RequestStatusCodes.Executed)
        {
            return MapToDto(requestEntity);
        }

        if (currentStatus != RequestStatusCodes.Approved)
        {
            throw new InvalidOperationException("Solo se pueden ejecutar solicitudes aprobadas.");
        }

        var executedStatus = await _context.RequestStatuses.FirstOrDefaultAsync(
            status => status.Code == RequestStatusCodes.Executed,
            cancellationToken);

        if (executedStatus is null)
        {
            throw new KeyNotFoundException("Estado de solicitud no encontrado.");
        }

        var requestTypeCode = requestEntity.RequestType?.Code ?? string.Empty;
        if (requestTypeCode == RequestTypeCodes.Purchase)
        {
            await ExecutePurchaseAsync(requestEntity, actorId, request.Notes, cancellationToken);
        }
        else if (requestTypeCode == RequestTypeCodes.Transfer)
        {
            await ExecuteTransferAsync(requestEntity, actorId, request.Notes, cancellationToken);
        }
        else
        {
            throw new InvalidOperationException("Tipo de solicitud invalido.");
        }

        requestEntity.StatusId = executedStatus.Id;
        requestEntity.ExecutedAt = DateTimeOffset.UtcNow;
        requestEntity.Observations = string.IsNullOrWhiteSpace(requestEntity.Observations)
            ? request.Notes?.Trim()
            : string.IsNullOrWhiteSpace(request.Notes?.Trim())
                ? requestEntity.Observations
                : $"{requestEntity.Observations} | {request.Notes!.Trim()}";

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(requestEntity);
    }

    private async Task ExecutePurchaseAsync(
        InternalRequest requestEntity,
        Guid actorId,
        string? notes,
        CancellationToken cancellationToken)
    {
        var targetBranchId = requestEntity.TargetBranchId;
        if (!targetBranchId.HasValue)
        {
            throw new KeyNotFoundException("Sede destino no encontrada.");
        }

        var branch = await _context.Branches.FirstOrDefaultAsync(
            branch => branch.Id == targetBranchId.Value && branch.IsActive,
            cancellationToken);

        if (branch is null)
        {
            throw new KeyNotFoundException("Sede destino no encontrada.");
        }

        var movementType = await GetPurchaseMovementTypeAsync(cancellationToken);

        foreach (var item in requestEntity.Items)
        {
            if (item.BookId is null)
            {
                throw new KeyNotFoundException("Libro no encontrado.");
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException("La cantidad debe ser mayor a 0.");
            }

            if (item.Book is null || item.Book.IsDeleted || !item.Book.IsActive)
            {
                throw new KeyNotFoundException("Libro no encontrado.");
            }

            var stock = await _context.InventoryStocks.FirstOrDefaultAsync(
                stock => stock.BookId == item.BookId.Value && stock.BranchId == targetBranchId.Value,
                cancellationToken);

            if (stock is null)
            {
                stock = new InventoryStock
                {
                    BookId = item.BookId.Value,
                    BranchId = targetBranchId.Value,
                    CurrentStock = 0,
                    MinStock = 0,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };

                _context.InventoryStocks.Add(stock);
            }

            var previousStock = stock.CurrentStock;
            var newStock = previousStock + item.Quantity;

            stock.CurrentStock = newStock;
            stock.UpdatedAt = DateTimeOffset.UtcNow;

            _context.InventoryMovements.Add(
                new InventoryMovement
                {
                    BookId = item.BookId.Value,
                    BranchId = targetBranchId.Value,
                    MovementTypeId = movementType.Id,
                    Quantity = item.Quantity,
                    PreviousStock = previousStock,
                    NewStock = newStock,
                    Reason = NormalizeMovementReason(notes),
                    ActorId = actorId,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
        }
    }

    private async Task ExecuteTransferAsync(
        InternalRequest requestEntity,
        Guid actorId,
        string? notes,
        CancellationToken cancellationToken)
    {
        var sourceBranchId = requestEntity.SourceBranchId;
        var targetBranchId = requestEntity.TargetBranchId;

        if (!sourceBranchId.HasValue || !targetBranchId.HasValue)
        {
            throw new KeyNotFoundException("Sede de origen y destino no encontradas.");
        }

        if (sourceBranchId.Value == targetBranchId.Value)
        {
            throw new ArgumentException("Las sedes de origen y destino deben ser diferentes.");
        }

        var sourceBranch = await _context.Branches.FirstOrDefaultAsync(
            branch => branch.Id == sourceBranchId.Value && branch.IsActive,
            cancellationToken);

        if (sourceBranch is null)
        {
            throw new KeyNotFoundException("Sede de origen no encontrada.");
        }

        var targetBranch = await _context.Branches.FirstOrDefaultAsync(
            branch => branch.Id == targetBranchId.Value && branch.IsActive,
            cancellationToken);

        if (targetBranch is null)
        {
            throw new KeyNotFoundException("Sede de destino no encontrada.");
        }

        var movementOutType = await GetTransferOutMovementTypeAsync(cancellationToken);
        var movementInType = await GetTransferInMovementTypeAsync(cancellationToken);

        var inventoryItems = requestEntity.Items.ToList();

        foreach (var item in inventoryItems)
        {
            if (item.BookId is null)
            {
                throw new KeyNotFoundException("Libro no encontrado.");
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException("La cantidad debe ser mayor a 0.");
            }

            if (item.Book is null || item.Book.IsDeleted || !item.Book.IsActive)
            {
                throw new KeyNotFoundException("Libro no encontrado.");
            }

            var sourceStock = await _context.InventoryStocks.FirstOrDefaultAsync(
                stock => stock.BookId == item.BookId.Value && stock.BranchId == sourceBranchId.Value,
                cancellationToken);

            if (sourceStock is null || sourceStock.CurrentStock < item.Quantity)
            {
                throw new InvalidOperationException("Stock insuficiente para ejecutar la solicitud.");
            }
        }

        foreach (var item in inventoryItems)
        {
            var previousDestinationStock = 0;

            var sourceStock = await _context.InventoryStocks.FirstAsync(
                stock => stock.BookId == item.BookId!.Value && stock.BranchId == sourceBranchId.Value,
                cancellationToken);

            var previousSourceStock = sourceStock.CurrentStock;
            var newSourceStock = previousSourceStock - item.Quantity;
            sourceStock.CurrentStock = newSourceStock;
            sourceStock.UpdatedAt = DateTimeOffset.UtcNow;

            var destinationStock = await _context.InventoryStocks.FirstOrDefaultAsync(
                stock => stock.BookId == item.BookId!.Value && stock.BranchId == targetBranchId.Value,
                cancellationToken);

            if (destinationStock is null)
            {
                destinationStock = new InventoryStock
                {
                    BookId = item.BookId!.Value,
                    BranchId = targetBranchId.Value,
                    CurrentStock = 0,
                    MinStock = 0,
                    UpdatedAt = DateTimeOffset.UtcNow,
                };

                _context.InventoryStocks.Add(destinationStock);
            }

            previousDestinationStock = destinationStock.CurrentStock;
            var newDestinationStock = previousDestinationStock + item.Quantity;

            destinationStock.CurrentStock = newDestinationStock;
            destinationStock.UpdatedAt = DateTimeOffset.UtcNow;

            var movementReason = NormalizeMovementReason(notes);

            _context.InventoryMovements.Add(
                new InventoryMovement
                {
                    BookId = item.BookId!.Value,
                    BranchId = sourceBranchId.Value,
                    MovementTypeId = movementOutType.Id,
                    Quantity = item.Quantity,
                    PreviousStock = previousSourceStock,
                    NewStock = newSourceStock,
                    Reason = movementReason,
                    ActorId = actorId,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

            _context.InventoryMovements.Add(
                new InventoryMovement
                {
                    BookId = item.BookId!.Value,
                    BranchId = targetBranchId.Value,
                    MovementTypeId = movementInType.Id,
                    Quantity = item.Quantity,
                    PreviousStock = previousDestinationStock,
                    NewStock = newDestinationStock,
                    Reason = movementReason,
                    ActorId = actorId,
                    CreatedAt = DateTimeOffset.UtcNow,
                });
        }
    }

    private async Task<InventoryMovementType> GetPurchaseMovementTypeAsync(CancellationToken cancellationToken)
    {
        var preferredMovementType = await _context.InventoryMovementTypes.FirstOrDefaultAsync(
            movementType => movementType.Code == InventoryMovementTypeCodes.Entry,
            cancellationToken);

        if (preferredMovementType is not null)
        {
            return preferredMovementType;
        }

        var fallbackMovementType = await _context.InventoryMovementTypes.FirstOrDefaultAsync(
            movementType => movementType.Code == InventoryMovementTypeCodes.Entry,
            cancellationToken);

        return fallbackMovementType ?? throw new InvalidOperationException("Tipo de movimiento de entrada no configurado.");
    }

    private async Task<InventoryMovementType> GetTransferOutMovementTypeAsync(CancellationToken cancellationToken)
    {
        var preferredMovementType = await _context.InventoryMovementTypes.FirstOrDefaultAsync(
            movementType => movementType.Code == InventoryMovementTypeCodes.TransferOut,
            cancellationToken);

        if (preferredMovementType is not null)
        {
            return preferredMovementType;
        }

        var fallbackMovementType = await _context.InventoryMovementTypes.FirstOrDefaultAsync(
            movementType => movementType.Code == InventoryMovementTypeCodes.Exit,
            cancellationToken);

        return fallbackMovementType ?? throw new InvalidOperationException("Tipo de movimiento de salida no configurado.");
    }

    private async Task<InventoryMovementType> GetTransferInMovementTypeAsync(CancellationToken cancellationToken)
    {
        var preferredMovementType = await _context.InventoryMovementTypes.FirstOrDefaultAsync(
            movementType => movementType.Code == InventoryMovementTypeCodes.TransferIn,
            cancellationToken);

        if (preferredMovementType is not null)
        {
            return preferredMovementType;
        }

        var fallbackMovementType = await _context.InventoryMovementTypes.FirstOrDefaultAsync(
            movementType => movementType.Code == InventoryMovementTypeCodes.Entry,
            cancellationToken);

        return fallbackMovementType ?? throw new InvalidOperationException("Tipo de movimiento de entrada no configurado.");
    }

    private static string? NormalizeMovementReason(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var reason = notes!.Trim();
        return reason.Length > 250 ? reason.Substring(0, 250) : reason;
    }

    private static InternalRequestDto MapToDto(InternalRequest requestEntity)
    {
        var statusCode = requestEntity.Status?.Code ?? string.Empty;

        return new InternalRequestDto
        {
            Id = requestEntity.Id,
            RequestTypeCode = requestEntity.RequestType?.Code ?? string.Empty,
            RequestTypeName = requestEntity.RequestType?.Name ?? string.Empty,
            StatusCode = statusCode,
            StatusName = requestEntity.Status?.Name ?? string.Empty,
            RequestedByUserId = requestEntity.ActorId,
            RequestedByUserName = requestEntity.Actor?.FullName ?? string.Empty,
            SourceBranchId = requestEntity.SourceBranchId,
            SourceBranchName = requestEntity.SourceBranch?.Name,
            DestinationBranchId = requestEntity.TargetBranchId,
            DestinationBranchName = requestEntity.TargetBranch?.Name,
            Notes = requestEntity.Description,
            CreatedAt = requestEntity.CreatedAt,
            UpdatedAt = requestEntity.ExecutedAt ?? requestEntity.ReviewedAt ?? requestEntity.CreatedAt,
            ApprovedAt = statusCode == RequestStatusCodes.Approved ? requestEntity.ReviewedAt : null,
            RejectedAt = statusCode == RequestStatusCodes.Rejected ? requestEntity.ReviewedAt : null,
            ExecutedAt = requestEntity.ExecutedAt,
            Items = requestEntity.Items
                .Select(item => new InternalRequestItemDto
                {
                    Id = item.Id,
                    BookId = item.BookId ?? Guid.Empty,
                    BookTitle = item.RequestedTitle ?? item.Book?.Title ?? string.Empty,
                    Isbn = item.Book?.Isbn,
                    Quantity = item.Quantity,
                })
                .ToList(),
        };
    }
}
