using Application.Features.Catalog.Authors;
using Application.Features.Catalog.Authors.ActivateAuthor;
using Application.Features.Catalog.Authors.CreateAuthor;
using Application.Features.Catalog.Authors.DisableAuthor;
using Application.Features.Catalog.Authors.GetAuthorById;
using Application.Features.Catalog.Authors.GetAuthors;
using Application.Features.Catalog.Authors.UpdateAuthor;
using Api.Contracts.Catalog.Authors;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/autores")]
public sealed class AuthorsController : ControllerBase
{
    private readonly ISender _sender;

    public AuthorsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetAuthors(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAuthorsQuery { IncludeInactive = includeInactive }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAuthorById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAuthorByIdQuery { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Recurso no encontrado." });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.BooksCreate)]
    public async Task<IActionResult> CreateAuthor(
        [FromBody] CreateAuthorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new CreateAuthorCommand
                {
                    FullName = request.FullName,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetAuthorById), new { id = result.Id }, result);
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
    [Authorize(Policy = PermissionCodes.BooksUpdate)]
    public async Task<IActionResult> UpdateAuthor(
        Guid id,
        [FromBody] UpdateAuthorRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new UpdateAuthorCommand
                {
                    Id = id,
                    FullName = request.FullName,
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/desactivar")]
    [Authorize(Policy = PermissionCodes.BooksUpdate)]
    public async Task<IActionResult> DisableAuthor(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new DisableAuthorCommand { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Recurso no encontrado." });
        }

        return Ok(result);
    }

    [HttpPatch("{id:guid}/activar")]
    [Authorize(Policy = PermissionCodes.BooksUpdate)]
    public async Task<IActionResult> ActivateAuthor(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ActivateAuthorCommand { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Recurso no encontrado." });
        }

        return Ok(result);
    }
}

