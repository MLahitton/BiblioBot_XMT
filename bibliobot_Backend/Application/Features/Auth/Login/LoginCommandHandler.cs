using Application.Common.Interfaces;
using Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Features.Auth.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly int _refreshTokenDays;

    public LoginCommandHandler(
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

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("El correo es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("La contrasenia es obligatoria.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(
            existing => existing.Email == email,
            cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("INVALID_CREDENTIALS");
        }

        if (!user.IsActive || user.IsDeleted)
        {
            throw new InvalidOperationException("USER_INACTIVE");
        }

        if (!_passwordHasher.VerifyPassword(user, request.Password))
        {
            throw new UnauthorizedAccessException("INVALID_CREDENTIALS");
        }

        var refreshToken = _refreshTokenService.GenerateRefreshToken();
        var refreshTokenHash = _refreshTokenService.HashRefreshToken(refreshToken);
        var now = DateTimeOffset.UtcNow;

        var refreshTokenEntity = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = now.AddDays(_refreshTokenDays),
            CreatedAt = now
        };

        var userRoleQuery = _context.UserRoles
            .Where(userRole => userRole.UserId == user.Id);

        var roleIds = await userRoleQuery
            .Select(userRole => userRole.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await userRoleQuery
            .Join(_context.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Code)
            .ToListAsync(cancellationToken);

        var permissions = await GetPermissionsForRoleIdsAsync(roleIds, cancellationToken);
        var accessToken = _jwtTokenGenerator.GenerateToken(user, roles, permissions);

        if (_context is Microsoft.EntityFrameworkCore.DbContext dbContext)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
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
                Roles = roles,
                Permissions = permissions
            }
        };
    }

    private async Task<IReadOnlyCollection<string>> GetPermissionsForRoleIdsAsync(
        IEnumerable<Guid> roleIds,
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
