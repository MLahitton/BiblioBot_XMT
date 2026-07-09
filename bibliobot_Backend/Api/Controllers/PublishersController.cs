using Application.Features.Catalog.Publishers.ActivatePublisher;
using Application.Features.Catalog.Publishers.CreatePublisher;
using Application.Features.Catalog.Publishers.DisablePublisher;
using Application.Features.Catalog.Publishers.GetPublishers;
using Application.Features.Catalog.Publishers.GetPublisherById;
using Application.Features.Catalog.Publishers.UpdatePublisher;
using Api.Contracts.Catalog.Publishers;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/editoriales")]
public sealed class PublishersController : ControllerBase
{
    private readonly ISender _sender;

    public PublishersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublishers(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetPublishersQuery { IncludeInactive = includeInactive }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPublisherById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetPublisherByIdQuery { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Recurso no encontrado." });
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.BooksCreate)]
    public async Task<IActionResult> CreatePublisher(
        [FromBody] CreatePublisherRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new CreatePublisherCommand
                {
                    Name = request.Name,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetPublisherById), new { id = result.Id }, result);
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
    public async Task<IActionResult> UpdatePublisher(
        Guid id,
        [FromBody] UpdatePublisherRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new UpdatePublisherCommand
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
    public async Task<IActionResult> DisablePublisher(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new DisablePublisherCommand { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Recurso no encontrado." });
        }

        return Ok(result);
    }

    [HttpPatch("{id:guid}/activar")]
    [Authorize(Policy = PermissionCodes.BooksUpdate)]
    public async Task<IActionResult> ActivatePublisher(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ActivatePublisherCommand { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Recurso no encontrado." });
        }

        return Ok(result);
    }
}
