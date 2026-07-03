using System.Security.Cryptography;
using System.Text;

using Application.Common.Interfaces;

namespace Infrastructure.Security;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public string HashRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ArgumentException("Refresh token is required.", nameof(refreshToken));
        }

        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(tokenBytes);

        return Convert.ToBase64String(hash);
    }
}
