using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.GetAdminUserById;

public sealed class GetAdminUserByIdQueryHandler : IRequestHandler<GetAdminUserByIdQuery, AdminUserDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAdminUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminUserDetailDto?> Handle(
        GetAdminUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var basicData = await _context.Users.AsNoTracking()
            .Where(user => user.Id == request.Id && !user.IsDeleted)
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

