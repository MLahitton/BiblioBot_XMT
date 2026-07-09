using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchRoles;

public sealed class SearchRolesLookupQueryHandler
    : IRequestHandler<SearchRolesLookupQuery, PagedResult<LookupRoleDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchRolesLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupRoleDto>> Handle(
        SearchRolesLookupQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.Roles.AsNoTracking();

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            query = query.Where(role =>
                role.Code.ToUpper().Contains(normalized) ||
                role.Name.ToUpper().Contains(normalized));
        }

        var code = request.Code?.Trim();
        if (!string.IsNullOrWhiteSpace(code))
        {
            var normalizedCode = code!.ToUpperInvariant();
            query = query.Where(role => role.Code == normalizedCode);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(role => role.Code)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(role => new LookupRoleDto
            {
                Id = role.Id,
                Code = role.Code,
                Name = role.Name,
                Label = $"{role.Code} - {role.Name}",
                PermissionsCount = role.RolePermissions.Count(),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupRoleDto>(items, pageNumber, pageSize, totalCount);
    }
}

