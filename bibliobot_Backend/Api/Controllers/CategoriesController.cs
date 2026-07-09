using Application.Features.Catalog.Categories.ActivateCategory;
using Application.Features.Catalog.Categories.CreateCategory;
using Application.Features.Catalog.Categories.DisableCategory;
using Application.Features.Catalog.Categories.GetCategories;
using Application.Features.Catalog.Categories.GetCategoryById;
using Application.Features.Catalog.Categories.UpdateCategory;
using Api.Contracts.Catalog.Categories;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/categorias")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetCategoriesQuery { IncludeInactive = includeInactive }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategoryById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetCategoryByIdQuery { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Recurso no encontrado." });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.BooksCreate)]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new CreateCategoryCommand
                {
                    Name = request.Name,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetCategoryById), new { id = result.Id }, result);
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
    public async Task<IActionResult> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new UpdateCategoryCommand
                {
                    Id = id,
                    Name = request.Name,
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
    public async Task<IActionResult> DisableCategory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new DisableCategoryCommand { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Recurso no encontrado." });
        }

        return Ok(result);
    }

    [HttpPatch("{id:guid}/activar")]
    [Authorize(Policy = PermissionCodes.BooksUpdate)]
    public async Task<IActionResult> ActivateCategory(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ActivateCategoryCommand { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Recurso no encontrado." });
        }

        return Ok(result);
    }
}

