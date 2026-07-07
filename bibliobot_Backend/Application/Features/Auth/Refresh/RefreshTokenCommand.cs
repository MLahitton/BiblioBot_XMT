using Application.Common.Security;
using MediatR;

namespace Application.Features.Auth.Refresh;

public sealed class RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string RefreshToken { get; init; } = string.Empty;
}
