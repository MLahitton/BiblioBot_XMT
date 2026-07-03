namespace Application.Common.Security;

public sealed class AuthResponseDto
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public AuthUserDto User { get; init; } = null!;
}
