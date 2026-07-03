using System.Net.Mail;

using Application.Common.Interfaces;
using Application.Common.Security;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Features.Auth.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly int _refreshTokenDays;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenService refreshTokenService,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenService = refreshTokenService;
        _refreshTokenDays = GetRefreshTokenDays(configuration);
    }

    private static int GetRefreshTokenDays(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return int.TryParse(configuration["Jwt:RefreshTokenDays"], out var days) && days > 0 ? days : 7;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var phone = request.Phone?.Trim();
        var documentNumber = request.DocumentNumber?.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
        {
            throw new ArgumentException("El nombre completo es obligatorio y debe tener maximo 150 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(email) || email.Length > 180)
        {
            throw new ArgumentException("El correo es obligatorio y debe tener maximo 180 caracteres.");
        }

        if (!IsValidEmail(email))
        {
            throw new ArgumentException("El formato del correo no es valido.");
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            throw new ArgumentException("La contrasenia debe tener al menos 8 caracteres.");
        }

        if (phone is not null && phone.Length > 40)
        {
            throw new ArgumentException("El telefono debe tener maximo 40 caracteres.");
        }

        if (documentNumber is not null && documentNumber.Length > 50)
        {
            throw new ArgumentException("El documento debe tener maximo 50 caracteres.");
        }

        var emailExists = await _context.Users.AnyAsync(user => user.Email == email, cancellationToken);
        if (emailExists)
        {
            throw new InvalidOperationException("EMAIL_ALREADY_EXISTS");
        }

        var clientRole = await _context.Roles.FirstOrDefaultAsync(
            role => role.Code == RoleCodes.Client && role.IsActive,
            cancellationToken);

        if (clientRole is null)
        {
            throw new InvalidOperationException("CLIENT_ROLE_NOT_FOUND");
        }

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = string.Empty,
            Phone = phone,
            DocumentNumber = documentNumber,
            IsActive = true,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        var refreshToken = _refreshTokenService.GenerateRefreshToken();
        var refreshTokenHash = _refreshTokenService.HashRefreshToken(refreshToken);

        var now = DateTimeOffset.UtcNow;
        var refreshTokenEntity = new RefreshToken
        {
            User = user,
            TokenHash = refreshTokenHash,
            ExpiresAt = now.AddDays(_refreshTokenDays),
            CreatedAt = now
        };

        var role = new UserRole
        {
            User = user,
            Role = clientRole
        };

        var permissionCodes = await GetPermissionsForRoleIdsAsync([clientRole.Id], cancellationToken);

        var accessToken = _jwtTokenGenerator.GenerateToken(
            user,
            [clientRole.Code],
            permissionCodes);

        if (_context is Microsoft.EntityFrameworkCore.DbContext dbContext)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.Users.Add(user);
                _context.UserRoles.Add(role);
                _context.RefreshTokens.Add(refreshTokenEntity);

                await _context.SaveChangesAsync(cancellationToken);
                await dbContext.Database.CommitTransactionAsync(cancellationToken);
            }
            catch
            {
                await dbContext.Database.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
        else
        {
            _context.Users.Add(user);
            _context.UserRoles.Add(role);
            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new AuthUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                DocumentNumber = user.DocumentNumber,
                Roles = [clientRole.Code],
                Permissions = permissionCodes
            }
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<IReadOnlyCollection<string>> GetPermissionsForRoleIdsAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        var permissions = await _context.RolePermissions
            .Where(rolePermission => roleIds.Contains(rolePermission.RoleId))
            .Join(
                _context.Permissions,
                rolePermission => rolePermission.PermissionId,
                permission => permission.Id,
                (_, permission) => permission)
            .Where(permission => permission.IsActive)
            .Select(permission => permission.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions;
    }
}
