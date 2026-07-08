using System.Security.Claims;
using Application.Features.Admin.ActivateAdminUser;
using Application.Features.Admin.DeactivateAdminUser;
using Application.Features.Admin.CreateAdminProduct;
using Application.Features.Admin.CreateAdminUser;
using Application.Features.Admin.DeleteAdminProduct;
using Application.Features.Admin.DeleteAdminUser;
using Application.Features.Admin.AssignUserRole;
using Application.Features.Admin.GetAdminPermissions;
using Application.Features.Admin.GetAdminProducts;
using Application.Features.Admin.GetAdminRoles;
using Application.Features.Admin.GetAdminUserById;
using Application.Features.Admin.GetAdminUsers;
using Application.Features.Admin.RemoveUserRole;
using Application.Features.Admin.UpdateAdminProduct;
using Application.Features.Admin.UpdateAdminUser;
using Application.Features.Books.ActivateBook;
using Application.Features.Books.DisableBook;
using Api.Contracts.Admin;
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

    [HttpPost("usuarios")]
    [Authorize]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo autenticar al usuario." });
        }

        if (!HasPermission(PermissionCodes.AdminUsersRead) || !HasPermission(PermissionCodes.AdminRolesRead))
        {
            return Forbid();
        }

        try
        {
            var result = await _sender.Send(
                new CreateAdminUserCommand
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Password = request.Password,
                    Phone = request.Phone,
                    DocumentNumber = request.DocumentNumber,
                    RoleCodes = request.RoleCodes,
                    ActorUserId = actorId,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
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

    [HttpPut("usuarios/{id:guid}")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    [Authorize(Policy = PermissionCodes.AdminRolesRead)]
    public async Task<IActionResult> UpdateUser(
        [FromRoute] Guid id,
        [FromBody] UpdateAdminUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo autenticar al usuario." });
        }

        try
        {
            var result = await _sender.Send(
                new UpdateAdminUserCommand
                {
                    Id = id,
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    DocumentNumber = request.DocumentNumber,
                    RoleCodes = request.RoleCodes,
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

    [HttpDelete("usuarios/{id:guid}")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> DeleteUser(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo autenticar al usuario." });
        }

        try
        {
            var deleted = await _sender.Send(
                new DeleteAdminUserCommand
                {
                    Id = id,
                    ActorUserId = actorId,
                },
                cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = "Usuario no encontrado." });
            }

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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

    [HttpGet("roles")]
    [Authorize(Policy = PermissionCodes.AdminRolesRead)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAdminRolesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("productos")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAdminProductsQuery
            {
                Search = search,
                IsActive = isActive,
                SortBy = sortBy,
                PageNumber = pageNumber,
                PageSize = pageSize,
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("productos")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateAdminProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new CreateAdminProductCommand
                {
                    Title = request.Title,
                    Isbn = request.Isbn,
                    Description = request.Description,
                    PublisherName = request.PublisherName,
                    PublicationYear = request.PublicationYear,
                    Language = request.Language,
                    ImageUrl = request.ImageUrl,
                    Price = request.Price,
                    AuthorNames = request.AuthorNames,
                    CategoryNames = request.CategoryNames,
                    BranchId = request.BranchId,
                    CurrentStock = request.CurrentStock,
                    MinStock = request.MinStock,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetProducts), new { id = result.Id }, result);
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

    [HttpPut("productos/{id:guid}")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> UpdateProduct(
        [FromRoute] Guid id,
        [FromBody] UpdateAdminProductRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new UpdateAdminProductCommand
                {
                    Id = id,
                    Title = request.Title,
                    Isbn = request.Isbn,
                    Description = request.Description,
                    PublisherName = request.PublisherName,
                    PublicationYear = request.PublicationYear,
                    Language = request.Language,
                    ImageUrl = request.ImageUrl,
                    Price = request.Price,
                    AuthorNames = request.AuthorNames,
                    CategoryNames = request.CategoryNames,
                    BranchId = request.BranchId,
                    CurrentStock = request.CurrentStock,
                    MinStock = request.MinStock,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Producto no encontrado." });
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
    }

    [HttpPatch("productos/{id:guid}/activar")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> ActivateProduct(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new ActivateBookCommand { Id = id }, cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Producto no encontrado." });
            }

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("productos/{id:guid}/desactivar")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> DeactivateProduct(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new DisableBookCommand { Id = id }, cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Producto no encontrado." });
            }

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("productos/{id:guid}")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> DeleteProduct(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var deleted = await _sender.Send(
                new DeleteAdminProductCommand { Id = id },
                cancellationToken);

            if (!deleted)
            {
                return NotFound(new { message = "Producto no encontrado." });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
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

    [HttpPost("usuarios/{id:guid}/roles")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    [Authorize(Policy = PermissionCodes.AdminRolesRead)]
    public async Task<IActionResult> AssignUserRole(
        [FromRoute] Guid id,
        [FromBody] AssignUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo autenticar al usuario." });
        }

        try
        {
            var result = await _sender.Send(
                new AssignUserRoleCommand
                {
                    UserId = id,
                    RoleCode = request.RoleCode,
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

    [HttpDelete("usuarios/{id:guid}/roles/{roleCode}")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    [Authorize(Policy = PermissionCodes.AdminRolesRead)]
    public async Task<IActionResult> RemoveUserRole(
        [FromRoute] Guid id,
        [FromRoute] string roleCode,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetAuthenticatedUserId(out var actorId))
        {
            return Unauthorized(new { message = "No se pudo autenticar al usuario." });
        }

        try
        {
            var result = await _sender.Send(
                new RemoveUserRoleCommand
                {
                    UserId = id,
                    RoleCode = roleCode,
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

    private bool HasPermission(string permission)
        => User.HasClaim("permission", permission);
}
