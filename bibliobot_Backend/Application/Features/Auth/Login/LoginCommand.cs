using Application.Common.Security;
using MediatR;

namespace Application.Features.Auth.Login;

public sealed class LoginCommand : IRequest<AuthResponseDto>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
