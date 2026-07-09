using Application.Features.Books.GetBookById;
using Application.Features.Books.CreateBook;
using Application.Features.Books.UpdateBook;
using Application.Features.Books.DisableBook;
using Application.Features.Books.ActivateBook;
using Application.Features.Books.GetBooks;
using Application.Features.Books.SearchBooks;
using Application.Features.FavoriteBooks.AddFavoriteBook;
using Application.Features.FavoriteBooks.GetFavoriteBookStatus;
using Application.Features.FavoriteBooks.ListFavoriteBooks;
using Application.Features.FavoriteBooks.RemoveFavoriteBook;
using Api.Contracts.Books;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/libros")]
public sealed class BooksController : ControllerBase
{
    private readonly ISender _sender;

    public BooksController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.BooksCreate)]
    public async Task<IActionResult> CreateBook(
        [FromBody] CreateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new CreateBookCommand
                {
                    Title = request.Title,
                    Isbn = request.Isbn,
                    Description = request.Description,
                    PublisherId = request.PublisherId,
                    PublicationYear = request.PublicationYear,
                    Language = request.Language,
                    ImageUrl = request.ImageUrl,
                    Price = request.Price,
                    AuthorIds = request.AuthorIds,
                    CategoryIds = request.CategoryIds,
                },
                cancellationToken);

            return CreatedAtAction(nameof(GetBookById), new { id = result.Id }, result);
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

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionCodes.BooksUpdate)]
    public async Task<IActionResult> UpdateBook(
        Guid id,
        [FromBody] UpdateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new UpdateBookCommand
                {
                    Id = id,
                    Title = request.Title,
                    Isbn = request.Isbn,
                    Description = request.Description,
                    PublisherId = request.PublisherId,
                    PublicationYear = request.PublicationYear,
                    Language = request.Language,
                    ImageUrl = request.ImageUrl,
                    Price = request.Price,
                    AuthorIds = request.AuthorIds,
                    CategoryIds = request.CategoryIds,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Libro no encontrado." });
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

    [HttpPatch("{id:guid}/desactivar")]
    [Authorize(Policy = PermissionCodes.BooksDisable)]
    public async Task<IActionResult> DisableBook(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new DisableBookCommand { Id = id }, cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Libro no encontrado." });
            }

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/activar")]
    [Authorize(Policy = PermissionCodes.BooksActivate)]
    public async Task<IActionResult> ActivateBook(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new ActivateBookCommand { Id = id }, cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Libro no encontrado." });
            }

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBooks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? authorId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetBooksQuery
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                CategoryId = categoryId,
                AuthorId = authorId
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBookById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetBookByIdQuery { Id = id }, cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Libro no encontrado." });
        }

        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchBooks(
        [FromQuery] string q,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
        {
            return BadRequest(new { message = "El criterio de búsqueda debe tener al menos 2 caracteres." });
        }

        var result = await _sender.Send(
            new SearchBooksQuery
            {
                Query = q.Trim(),
                PageNumber = pageNumber,
                PageSize = pageSize,
            },
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("favoritos")]
    [Authorize]
    public async Task<IActionResult> GetFavoriteBooks(
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListFavoriteBooksQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{bookId:guid}/favorito")]
    [Authorize]
    public async Task<IActionResult> GetFavoriteBookStatus(
        [FromRoute] Guid bookId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetFavoriteBookStatusQuery { BookId = bookId },
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{bookId:guid}/favorito")]
    [Authorize]
    public async Task<IActionResult> AddFavoriteBook(
        [FromRoute] Guid bookId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new AddFavoriteBookCommand { BookId = bookId },
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
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

    [HttpDelete("{bookId:guid}/favorito")]
    [Authorize]
    public async Task<IActionResult> RemoveFavoriteBook(
        [FromRoute] Guid bookId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var removed = await _sender.Send(
                new RemoveFavoriteBookCommand { BookId = bookId },
                cancellationToken);

            if (!removed)
            {
                return NotFound(new { message = "El libro no está en favoritos." });
            }

            return NoContent();
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
}
