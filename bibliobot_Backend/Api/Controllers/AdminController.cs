using System.Security.Claims;
using Application.Features.Admin.ActivateAdminUser;
using Application.Features.Admin.DeactivateAdminUser;
using Application.Features.Admin.GetAdminPermissions;
using Application.Features.Admin.GetAdminRoles;
using Application.Features.Admin.GetAdminUserById;
using Application.Features.Admin.GetAdminUsers;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _sender;

    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("usuarios")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? roleCode = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isEmailConfirmed = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAdminUsersQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Search = search,
                RoleCode = roleCode,
                IsActive = isActive,
                IsEmailConfirmed = isEmailConfirmed,
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("usuarios/{id:guid}")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> GetUserById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAdminUserByIdQuery { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }

        return Ok(result);
    }

    [HttpGet("roles")]
    [Authorize(Policy = PermissionCodes.AdminRolesRead)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAdminRolesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("permisos")]
    [Authorize(Policy = PermissionCodes.AdminPermissionsRead)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAdminPermissionsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPatch("usuarios/{id:guid}/activar")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> ActivateUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo autenticar al usuario." });
        }

        try
        {
            var result = await _sender.Send(
                new ActivateAdminUserCommand
                {
                    Id = id,
                    ActorUserId = actorId,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            return Ok(result);
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPatch("usuarios/{id:guid}/desactivar")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> DeactivateUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo autenticar al usuario." });
        }

        try
        {
            var result = await _sender.Send(
                new DeactivateAdminUserCommand
                {
                    Id = id,
                    ActorUserId = actorId,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            return Ok(result);
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
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    private bool TryGetAuthenticatedUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claim, out userId);
    }
}

