using Application.Common.Interfaces;
using Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Features.Auth.Refresh;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly int _refreshTokenDays;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenService refreshTokenService,
        IConfiguration configuration)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenService = refreshTokenService;
        _refreshTokenDays = GetRefreshTokenDays(configuration);
    }

    private static int GetRefreshTokenDays(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        return int.TryParse(configuration["Jwt:RefreshTokenDays"], out var days) && days > 0 ? days : 7;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedAccessException("INVALID_REFRESH_TOKEN");
        }

        var refreshTokenHash = _refreshTokenService.HashRefreshToken(request.RefreshToken);
        var now = DateTimeOffset.UtcNow;

        var tokenEntity = await _context.RefreshTokens.FirstOrDefaultAsync(
            token => token.TokenHash == refreshTokenHash,
            cancellationToken);

        if (tokenEntity is null || tokenEntity.RevokedAt is not null || tokenEntity.ExpiresAt <= now)
        {
            throw new UnauthorizedAccessException("INVALID_REFRESH_TOKEN");
        }

        var user = await _context.Users.FirstOrDefaultAsync(
            existing => existing.Id == tokenEntity.UserId,
            cancellationToken);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            throw new UnauthorizedAccessException("INVALID_REFRESH_TOKEN");
        }

        var roleIds = await _context.UserRoles
            .Where(userRole => userRole.UserId == user.Id)
            .Select(userRole => userRole.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await _context.UserRoles
            .Where(userRole => userRole.UserId == user.Id)
            .Join(_context.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Code)
            .Distinct()
            .ToListAsync(cancellationToken);

        var permissions = await GetPermissionsForRoleIdsAsync(roleIds, cancellationToken);
        var accessToken = _jwtTokenGenerator.GenerateToken(user, roles, permissions);

        var newRefreshToken = _refreshTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _refreshTokenService.HashRefreshToken(newRefreshToken);

        tokenEntity.RevokedAt = now;
        var newTokenEntity = new Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = now.AddDays(_refreshTokenDays),
            CreatedAt = now
        };

        if (_context is Microsoft.EntityFrameworkCore.DbContext dbContext)
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _context.RefreshTokens.Add(newTokenEntity);
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
            _context.RefreshTokens.Add(newTokenEntity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
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
