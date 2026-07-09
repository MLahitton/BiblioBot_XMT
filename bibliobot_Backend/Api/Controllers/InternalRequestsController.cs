using System.Security.Claims;
using Application.Features.InternalRequests.ApproveInternalRequest;
using Application.Features.InternalRequests.CreatePurchaseRequest;
using Application.Features.InternalRequests.CreateTransferRequest;
using Application.Features.InternalRequests.ExecuteInternalRequest;
using Application.Features.InternalRequests.GetInternalRequestById;
using Application.Features.InternalRequests.GetInternalRequests;
using Application.Features.InternalRequests.RejectInternalRequest;
using Api.Contracts.InternalRequests;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PurchaseItemCommand = Application.Features.InternalRequests.CreatePurchaseRequest.CreateInternalRequestItemCommand;
using TransferItemCommand = Application.Features.InternalRequests.CreateTransferRequest.CreateInternalRequestItemCommand;

namespace Api.Controllers;

[ApiController]
[Route("api/solicitudes")]
public sealed class InternalRequestsController : ControllerBase
{
    private readonly ISender _sender;

    public InternalRequestsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("compras")]
    [Authorize(Policy = PermissionCodes.RequestsPurchaseCreate)]
    public async Task<IActionResult> CreatePurchase(
        [FromBody] CreatePurchaseRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new CreatePurchaseRequestCommand
                {
                    BranchId = request.BranchId,
                    Notes = request.Notes,
                    Items = request.Items
                        .Select(item => new PurchaseItemCommand
                        {
                            BookId = item.BookId,
                            Quantity = item.Quantity,
                        })
                        .ToList(),
                },
                cancellationToken);

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
    }

    [HttpPost("traslados")]
    [Authorize(Policy = PermissionCodes.RequestsTransferCreate)]
    public async Task<IActionResult> CreateTransfer(
        [FromBody] CreateTransferRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new CreateTransferRequestCommand
                {
                    SourceBranchId = request.SourceBranchId,
                    DestinationBranchId = request.DestinationBranchId,
                    Notes = request.Notes,
                    Items = request.Items
                        .Select(item => new TransferItemCommand
                        {
                            BookId = item.BookId,
                            Quantity = item.Quantity,
                        })
                        .ToList(),
                },
                cancellationToken);

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

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetInternalRequests(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? requestTypeCode = null,
        [FromQuery] string? statusCode = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] Guid? requestedByUserId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!HasPermission(PermissionCodes.RequestsRead))
            {
                return Forbid();
            }

            var canReadAll = HasPermission(PermissionCodes.RequestsRead);
            var canReadOwn = canReadAll;

            if (!TryGetAuthenticatedUserId(out var actorId))
            {
                return Unauthorized(new { message = "No se pudo autenticar al usuario." });
            }

            var result = await _sender.Send(
                new GetInternalRequestsQuery
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    RequestTypeCode = requestTypeCode,
                    StatusCode = statusCode,
                    BranchId = branchId,
                    RequestedByUserId = requestedByUserId,
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
            return Unauthorized(new { message = "No autorizado." });
        }
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetInternalRequestById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!HasPermission(PermissionCodes.RequestsRead))
            {
                return Forbid();
            }

            if (!TryGetAuthenticatedUserId(out var actorId))
            {
                return Unauthorized(new { message = "No se pudo autenticar al usuario." });
            }

            var canReadAll = HasPermission(PermissionCodes.RequestsRead);
            var canReadOwn = canReadAll;

            var result = await _sender.Send(
                new GetInternalRequestByIdQuery
                {
                    Id = id,
                    CanReadAll = canReadAll,
                    CanReadOwn = canReadOwn,
                    CurrentUserId = actorId,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Solicitud no encontrada." });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "No autorizado." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/aprobar")]
    [Authorize(Policy = PermissionCodes.RequestsApprove)]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid id,
        [FromBody] ApproveInternalRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new ApproveInternalRequestCommand
                {
                    Id = id,
                    Notes = request.Notes,
                },
                cancellationToken);

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
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "No autorizado." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/rechazar")]
    [Authorize(Policy = PermissionCodes.RequestsReject)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] RejectInternalRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new RejectInternalRequestCommand
                {
                    Id = id,
                    Reason = request.Reason,
                },
                cancellationToken);

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
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "No autorizado." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/ejecutar")]
    [Authorize(Policy = PermissionCodes.RequestsExecute)]
    public async Task<IActionResult> Execute(
        [FromRoute] Guid id,
        [FromBody] ExecuteInternalRequestRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new ExecuteInternalRequestCommand
                {
                    Id = id,
                    Notes = request.Notes,
                },
                cancellationToken);

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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private bool HasPermission(string permission)
    {
        return User.Claims.Any(claim =>
            claim.Type == "permission" && claim.Value == permission);
    }

    private bool TryGetAuthenticatedUserId(out Guid userId)
    {
        userId = Guid.Empty;

        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claim, out userId);
    }
}
