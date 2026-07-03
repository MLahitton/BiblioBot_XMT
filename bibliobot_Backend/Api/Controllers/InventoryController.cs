using Application.Common.DTOs;
using Application.Features.Inventory.GetInventory;
using Application.Features.Inventory.GetInventoryMovements;
using Application.Features.Inventory.RegisterAdjustment;
using Application.Features.Inventory.RegisterEntry;
using Application.Features.Inventory.RegisterExit;
using Api.Contracts.Inventory;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/inventario")]
public sealed class InventoryController : ControllerBase
{
    private readonly ISender _sender;

    public InventoryController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    public async Task<IActionResult> GetInventory(
        [FromQuery] Guid? bookId = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetInventoryQuery
            {
                BookId = bookId,
                BranchId = branchId,
                LowStockOnly = lowStockOnly,
                PageNumber = pageNumber,
                PageSize = pageSize,
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("movimientos")]
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    public async Task<IActionResult> GetInventoryMovements(
        [FromQuery] Guid? bookId = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] string? movementTypeCode = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetInventoryMovementsQuery
            {
                BookId = bookId,
                BranchId = branchId,
                MovementTypeCode = movementTypeCode,
                PageNumber = pageNumber,
                PageSize = pageSize,
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("entradas")]
    [Authorize(Policy = PermissionCodes.InventoryEntry)]
    public async Task<IActionResult> RegisterEntry(
        [FromBody] RegisterInventoryEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new RegisterInventoryEntryCommand
                {
                    BookId = request.BookId,
                    BranchId = request.BranchId,
                    Quantity = request.Quantity,
                    Reason = request.Reason,
                    MinStock = request.MinStock,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Recurso no encontrado." });
            }

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("salidas")]
    [Authorize(Policy = PermissionCodes.InventoryExit)]
    public async Task<IActionResult> RegisterExit(
        [FromBody] RegisterInventoryExitRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new RegisterInventoryExitCommand
                {
                    BookId = request.BookId,
                    BranchId = request.BranchId,
                    Quantity = request.Quantity,
                    Reason = request.Reason,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Recurso no encontrado." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("ajustes")]
    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    public async Task<IActionResult> RegisterAdjustment(
        [FromBody] RegisterInventoryAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new RegisterInventoryAdjustmentCommand
                {
                    BookId = request.BookId,
                    BranchId = request.BranchId,
                    NewStock = request.NewStock,
                    Reason = request.Reason,
                    MinStock = request.MinStock,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Recurso no encontrado." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
