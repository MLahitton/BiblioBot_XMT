using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Application.Common.Interfaces;
using Domain.Entities;

namespace Infrastructure.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2";
    private const int Iterations = 120_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public string HashPassword(User user, string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password is required.", nameof(password));
        }

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = HashPasswordInternal(password, salt, Iterations);

        return string.Join(
            ':',
            Prefix,
            Iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(User user, string password)
    {
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        var parts = user.PasswordHash.Split(':');
        if (parts.Length != 4 || parts[0] != Prefix)
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var stored = Convert.FromBase64String(parts[3]);
            var computed = HashPasswordInternal(password, salt, iterations);

            if (stored.Length != computed.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(stored, computed);
        }
        catch
        {
            return false;
        }
    }

    private static byte[] HashPasswordInternal(string password, byte[] salt, int iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            HashSize);
    }
}
