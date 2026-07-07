using Api.Contracts.Auth;
using Application.Features.Auth.GetCurrentUser;
using Application.Features.Auth.Login;
using Application.Features.Auth.Refresh;
using Application.Features.Auth.Register;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new RegisterCommand
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Password = request.Password,
                    Phone = request.Phone,
                    DocumentNumber = request.DocumentNumber
                },
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMAIL_ALREADY_EXISTS")
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message == "CLIENT_ROLE_NOT_FOUND")
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new LoginCommand { Email = request.Email, Password = request.Password },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Credenciales invalidas." });
        }
        catch (InvalidOperationException ex) when (ex.Message == "USER_INACTIVE")
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Usuario inactivo." });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new RefreshTokenCommand { RefreshToken = request.RefreshToken },
                cancellationToken);

            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Token de refresco invalido o vencido." });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(new GetCurrentUserQuery(), cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "No se pudo autenticar al usuario." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Usuario no encontrado." });
        }
    }
}
