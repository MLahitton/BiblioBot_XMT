using Application.Common.Security;
using MediatR;

namespace Application.Features.Auth.Register;

public sealed class RegisterCommand : IRequest<AuthResponseDto>
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string? DocumentNumber { get; init; }
}
