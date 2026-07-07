using System.Security.Claims;
using Application.Features.Invoices.GetInvoiceById;
using Application.Features.Invoices.GetInvoiceBySaleId;
using Application.Features.Invoices.GetInvoices;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/facturas")]
public sealed class InvoicesController : ControllerBase
{
    private readonly ISender _sender;

    public InvoicesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? customerId = null,
        [FromQuery] Guid? saleId = null,
        [FromQuery] bool? isCancelled = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var actorId))
            {
                return Unauthorized(new { message = "No se pudo autenticar al usuario." });
            }

            var canReadAll = HasPermission(PermissionCodes.InvoicesReadAll);
            var canReadOwn = HasPermission(PermissionCodes.InvoicesReadOwn);

            if (!canReadAll && !canReadOwn)
            {
                return Forbid();
            }

            var result = await _sender.Send(
                new GetInvoicesQuery
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    CustomerId = canReadAll ? customerId : null,
                    SaleId = saleId,
                    IsCancelled = isCancelled,
                    From = from,
                    To = to,
                    CanReadAll = canReadAll,
                    CanReadOwn = canReadOwn,
                    CurrentUserId = actorId,
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var actorId))
            {
                return Unauthorized(new { message = "No se pudo autenticar al usuario." });
            }

            var canReadAll = HasPermission(PermissionCodes.InvoicesReadAll);
            var canReadOwn = HasPermission(PermissionCodes.InvoicesReadOwn);

            if (!canReadAll && !canReadOwn)
            {
                return Forbid();
            }

            var result = await _sender.Send(
                new GetInvoiceByIdQuery
                {
                    Id = id,
                    CanReadAll = canReadAll,
                    CanReadOwn = canReadOwn,
                    CurrentUserId = actorId,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Factura no encontrada." });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("venta/{saleId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetInvoiceBySaleId(
        [FromRoute] Guid saleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out var actorId))
            {
                return Unauthorized(new { message = "No se pudo autenticar al usuario." });
            }

            var canReadAll = HasPermission(PermissionCodes.InvoicesReadAll);
            var canReadOwn = HasPermission(PermissionCodes.InvoicesReadOwn);

            if (!canReadAll && !canReadOwn)
            {
                return Forbid();
            }

            var result = await _sender.Send(
                new GetInvoiceBySaleIdQuery
                {
                    SaleId = saleId,
                    CanReadAll = canReadAll,
                    CanReadOwn = canReadOwn,
                    CurrentUserId = actorId,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Factura no encontrada." });
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
