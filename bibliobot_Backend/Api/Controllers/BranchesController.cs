using Application.Features.Branches.ActivateBranch;
using Application.Features.Branches.CreateBranch;
using Application.Features.Branches.DisableBranch;
using Application.Features.Branches.GetBranchById;
using Application.Features.Branches.GetBranches;
using Application.Features.Branches.UpdateBranch;
using Api.Contracts.Branches;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/sedes")]
public sealed class BranchesController : ControllerBase
{
    private readonly ISender _sender;

    public BranchesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    public async Task<IActionResult> GetBranches(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetBranchesQuery { IncludeInactive = includeInactive }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    public async Task<IActionResult> GetBranchById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetBranchByIdQuery { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Sede no encontrada." });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    public async Task<IActionResult> CreateBranch(
        [FromBody] CreateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new CreateBranchCommand
                {
                    Name = request.Name,
                    Address = request.Address,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetBranchById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    public async Task<IActionResult> UpdateBranch(
        Guid id,
        [FromBody] UpdateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new UpdateBranchCommand
                {
                    Id = id,
                    Name = request.Name,
                    Address = request.Address,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Sede no encontrada." });
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
    }

    [HttpPatch("{id:guid}/desactivar")]
    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    public async Task<IActionResult> DisableBranch(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new DisableBranchCommand { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Sede no encontrada." });
        }

        return Ok(result);
    }

    [HttpPatch("{id:guid}/activar")]
    [Authorize(Policy = PermissionCodes.InventoryAdjust)]
    public async Task<IActionResult> ActivateBranch(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ActivateBranchCommand { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Sede no encontrada." });
        }

        return Ok(result);
    }
}

