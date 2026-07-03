using Application.Features.Cart.AddOrUpdateCartItem;
using Application.Features.Cart.ClearCart;
using Application.Features.Cart.GetCartBySession;
using Application.Features.Cart.RemoveCartItem;
using Api.Contracts.Cart;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/carrito")]
public sealed class CartController : ControllerBase
{
    private readonly ISender _sender;

    public CartController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Policy = PermissionCodes.CartManage)]
    public async Task<IActionResult> AddOrUpdateCartItem(
        [FromBody] AddOrUpdateCartItemRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var (cart, isCreated) = await _sender.Send(
                new AddOrUpdateCartItemCommand
                {
                    SessionId = request.SessionId,
                    BookId = request.BookId,
                    Quantity = request.Quantity,
                    BranchId = request.BranchId,
                },
                cancellationToken);

            if (cart is null)
            {
                return NotFound(new { message = "Carrito o item no encontrado." });
            }

            if (isCreated)
            {
                return StatusCode(StatusCodes.Status201Created, cart);
            }

            return Ok(cart);
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

    [HttpGet("{sessionId}")]
    [Authorize(Policy = PermissionCodes.CartRead)]
    public async Task<IActionResult> GetCartBySession(
        [FromRoute] string sessionId,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetCartBySessionQuery { SessionId = sessionId },
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{sessionId}/items/{bookId:guid}")]
    [Authorize(Policy = PermissionCodes.CartManage)]
    public async Task<IActionResult> RemoveCartItem(
        [FromRoute] string sessionId,
        [FromRoute] Guid bookId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new RemoveCartItemCommand
                {
                    SessionId = sessionId,
                    BookId = bookId,
                },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Carrito o item no encontrado." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpDelete("{sessionId}")]
    [Authorize(Policy = PermissionCodes.CartManage)]
    public async Task<IActionResult> ClearCart(
        [FromRoute] string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new ClearCartCommand { SessionId = sessionId },
                cancellationToken);

            if (result is null)
            {
                return NotFound(new { message = "Carrito o item no encontrado." });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
