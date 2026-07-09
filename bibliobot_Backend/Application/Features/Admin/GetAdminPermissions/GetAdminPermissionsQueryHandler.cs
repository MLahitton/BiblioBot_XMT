using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.GetAdminPermissions;

public sealed class GetAdminPermissionsQueryHandler : IRequestHandler<GetAdminPermissionsQuery, IReadOnlyCollection<AdminPermissionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminPermissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<AdminPermissionDto>> Handle(
        GetAdminPermissionsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Permissions.AsNoTracking()
            .OrderBy(permission => permission.Code)
            .Select(permission => new AdminPermissionDto
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                Description = permission.Description,
                IsActive = permission.IsActive,
            })
            .ToListAsync(cancellationToken);
    }
}

