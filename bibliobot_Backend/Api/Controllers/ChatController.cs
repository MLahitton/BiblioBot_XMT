using System.Security.Claims;
using Api.Contracts.Chat;
using Application.Features.Chat.SendChatMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private readonly ISender _sender;

    public ChatController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("message")]
    [Authorize]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!TryGetAuthenticatedUserId(out _))
            {
                return Unauthorized(new { message = "No se pudo autenticar al usuario." });
            }

            var result = await _sender.Send(
                new SendChatMessageCommand
                {
                    SessionId = request.SessionId,
                    Message = request.Message,
                    IsGuest = false,
                    RolesFromClaims = GetRolesFromClaims(),
                    PermissionsFromClaims = GetPermissionsFromClaims(),
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "No fue posible comunicarse con el servicio de chatbot." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                new { message = "El servicio de chatbot tardó demasiado en responder." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("public-message")]
    [AllowAnonymous]
    public async Task<IActionResult> SendPublicMessage(
        [FromBody] SendChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new SendChatMessageCommand
                {
                    SessionId = request.SessionId,
                    Message = request.Message,
                    IsGuest = true,
                    UserId = null,
                    UserEmail = null,
                    RolesFromClaims = ["GUEST"],
                    PermissionsFromClaims =
                    [
                        "chat.message",
                        "books.read",
                        "books.search"
                    ],
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = "No fue posible comunicarse con el servicio de chatbot." });
        }
        catch (TaskCanceledException)
        {
            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                new { message = "El servicio de chatbot tardó demasiado en responder." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new { message = ex.Message });
        }
    }

    private string[] GetRolesFromClaims()
    {
        return User.Claims
            .Where(claim => claim.Type == "role" || claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string[] GetPermissionsFromClaims()
    {
        return User.Claims
            .Where(claim => claim.Type == "permission")
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private bool TryGetAuthenticatedUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out userId);
    }
}
