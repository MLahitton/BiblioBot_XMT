using System.Security.Claims;
using Api.Contracts.Sales;
using Application.Features.Sales.ConfirmSale;
using Application.Features.Sales.CreateSale;
using Application.Features.Sales.GetSales;
using Application.Features.Sales.GetSaleById;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/ventas")]
public sealed class SalesController : ControllerBase
{
    private readonly ISender _sender;

    public SalesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.SalesCreate)]
    public async Task<IActionResult> CreateSale(
        [FromBody] CreateSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new CreateSaleCommand
                {
                    SessionId = request.SessionId,
                    BranchId = request.BranchId,
                    OriginCode = request.OriginCode,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetSaleById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/confirmar")]
    [Authorize(Policy = PermissionCodes.SalesConfirm)]
    public async Task<IActionResult> ConfirmSale(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new ConfirmSaleCommand { Id = id }, cancellationToken);
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetSales(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? statusCode = null,
        [FromQuery] string? originCode = null,
        [FromQuery] Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo autenticar al usuario." });
        }

        var canReadAll = HasPermission(PermissionCodes.SalesReadAll);
        var canReadOwn = HasPermission(PermissionCodes.SalesReadOwn);

        if (!canReadAll && !canReadOwn)
        {
            return Forbid();
        }

        var result = await _sender.Send(
            new GetSalesQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                StatusCode = statusCode,
                OriginCode = originCode,
                CustomerId = canReadAll ? customerId : null,
                CanReadAll = canReadAll,
                ActorId = actorId,
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetSaleById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var actorId))
            {
                return Unauthorized(new { message = "No se pudo autenticar al usuario." });
            }

            var canReadAll = HasPermission(PermissionCodes.SalesReadAll);
            var canReadOwn = HasPermission(PermissionCodes.SalesReadOwn);

            if (!canReadAll && !canReadOwn)
            {
                return Forbid();
            }

            var result = await _sender.Send(
                new GetSaleByIdQuery
                {
                    Id = id,
                    CanReadAll = canReadAll,
                    ActorId = actorId,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Venta no encontrada." });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private bool HasPermission(string permission)
    {
        return User.Claims.Any(claim => claim.Type == "permission" && claim.Value == permission);
    }

    private bool TryGetAuthenticatedUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
