using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.GetAdminRoles;

public sealed class GetAdminRolesQueryHandler : IRequestHandler<GetAdminRolesQuery, IReadOnlyCollection<AdminRoleDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminRolesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<AdminRoleDto>> Handle(
        GetAdminRolesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Roles.AsNoTracking()
            .OrderBy(role => role.Code)
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
    }
}

