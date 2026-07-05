using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.GetAdminUsers;

public sealed class GetAdminUsersQueryHandler : IRequestHandler<GetAdminUsersQuery, PagedResult<AdminUserListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AdminUserListItemDto>> Handle(
        GetAdminUsersQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _context.Users.AsNoTracking().Where(user => !user.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(user =>
                user.FullName.ToUpper().Contains(search) ||
                user.Email.ToUpper().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.RoleCode))
        {
            var roleCode = request.RoleCode!.Trim().ToUpperInvariant();
            query = query.Where(user => user.UserRoles.Any(userRole => userRole.Role.Code == roleCode));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(user => user.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(user => user.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new AdminUserListItemDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = user.UserRoles
                    .Select(userRole => userRole.Role.Code)
                    .Distinct()
                    .OrderBy(roleCode => roleCode)
                    .ToList(),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserListItemDto>(items, pageNumber, pageSize, totalCount);
    }
}

