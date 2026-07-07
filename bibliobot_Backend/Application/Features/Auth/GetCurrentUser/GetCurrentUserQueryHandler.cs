using Application.Common.Interfaces;
using Application.Common.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Auth.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, AuthUserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AuthUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("USER_UNAUTHENTICATED");
        }

        var userId = _currentUserService.UserId.Value;
        var user = await _context.Users.FirstOrDefaultAsync(
            existing => existing.Id == userId,
            cancellationToken);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            throw new KeyNotFoundException("USER_NOT_FOUND");
        }

        var roleIds = await _context.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Select(userRole => userRole.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await _context.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .Join(_context.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Code)
            .ToListAsync(cancellationToken);

        var permissions = await GetPermissionsForRoleIdsAsync(roleIds, cancellationToken);

        return new AuthUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            DocumentNumber = user.DocumentNumber,
            Roles = roles,
            Permissions = permissions
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
