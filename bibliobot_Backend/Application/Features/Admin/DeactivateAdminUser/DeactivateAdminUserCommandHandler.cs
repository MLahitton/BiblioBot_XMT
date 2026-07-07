using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.DeactivateAdminUser;

public sealed class DeactivateAdminUserCommandHandler : IRequestHandler<DeactivateAdminUserCommand, AdminUserDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public DeactivateAdminUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminUserDetailDto?> Handle(
        DeactivateAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty || request.ActorUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        if (request.Id == request.ActorUserId)
        {
            throw new ArgumentException("No puedes desactivarte a ti mismo.");
        }

        var actorIsActive = await _context.Users.AnyAsync(
            actor => actor.Id == request.ActorUserId && actor.IsActive && !actor.IsDeleted,
            cancellationToken);

        if (!actorIsActive)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(
            user => user.Id == request.Id && !user.IsDeleted,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (!user.IsActive)
        {
            return await GetAdminUserDetailAsync(user.Id, cancellationToken);
        }

        var adminRoleIds = await _context.Roles
            .Where(role => role.Code == RoleCodes.Admin && role.IsActive)
            .Select(role => role.Id)
            .ToListAsync(cancellationToken);

        if (adminRoleIds.Count > 0)
        {
            var userIsAdmin = await _context.UserRoles.AnyAsync(
                userRole => userRole.UserId == request.Id && adminRoleIds.Contains(userRole.RoleId),
                cancellationToken);

            if (userIsAdmin)
            {
                var activeAdmins = await _context.Users.AsNoTracking()
                    .Where(activeUser => activeUser.IsActive && !activeUser.IsDeleted)
                    .Where(activeUser => activeUser.UserRoles.Any(userRole => adminRoleIds.Contains(userRole.RoleId)))
                    .CountAsync(cancellationToken);

                if (activeAdmins <= 1)
                {
                    throw new InvalidOperationException("No se puede desactivar al último administrador activo.");
                }
            }
        }

        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetAdminUserDetailAsync(user.Id, cancellationToken);
    }

    private async Task<AdminUserDetailDto?> GetAdminUserDetailAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var basicData = await _context.Users.AsNoTracking()
            .Where(user => user.Id == userId && !user.IsDeleted)
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt,
                RoleIds = user.UserRoles.Select(userRole => userRole.RoleId).Distinct().ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (basicData is null)
        {
            return null;
        }

        var roleIds = basicData.RoleIds.ToList();
        var roles = roleIds.Count == 0
            ? []
            : await _context.Roles.AsNoTracking()
                .Where(role => roleIds.Contains(role.Id))
                .OrderBy(role => role.Name)
                .Select(role => new AdminRoleDto
                {
                    Id = role.Id,
                    Code = role.Code,
                    Name = role.Name,
                    Description = role.Description,
                    IsActive = role.IsActive,
                    PermissionsCount = role.RolePermissions.Count(),
                    Permissions = role.RolePermissions
                        .Select(rolePermission => new AdminPermissionDto
                        {
                            Id = rolePermission.Permission.Id,
                            Code = rolePermission.Permission.Code,
                            Name = rolePermission.Permission.Name,
                            Description = rolePermission.Permission.Description,
                            IsActive = rolePermission.Permission.IsActive,
                        })
                        .OrderBy(permission => permission.Code)
                        .ToList(),
                })
                .ToListAsync(cancellationToken);

        var effectivePermissionIds = roles
            .SelectMany(role => role.Permissions)
            .Select(permission => permission.Id)
            .Distinct()
            .ToList();

        var effectivePermissions = effectivePermissionIds.Count == 0
            ? []
            : await _context.Permissions.AsNoTracking()
                .Where(permission => effectivePermissionIds.Contains(permission.Id))
                .OrderBy(permission => permission.Name)
                .Select(permission => new AdminPermissionDto
                {
                    Id = permission.Id,
                    Code = permission.Code,
                    Name = permission.Name,
                    Description = permission.Description,
                    IsActive = permission.IsActive,
                })
                .ToListAsync(cancellationToken);

        return new AdminUserDetailDto
        {
            Id = basicData.Id,
            FullName = basicData.FullName,
            Email = basicData.Email,
            IsActive = basicData.IsActive,
            CreatedAt = basicData.CreatedAt,
            UpdatedAt = basicData.UpdatedAt,
            Roles = roles,
            EffectivePermissions = effectivePermissions,
        };
    }
}
