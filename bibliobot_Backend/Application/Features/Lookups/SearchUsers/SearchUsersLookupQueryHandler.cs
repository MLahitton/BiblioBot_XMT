using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchUsers;

public sealed class SearchUsersLookupQueryHandler
    : IRequestHandler<SearchUsersLookupQuery, PagedResult<LookupUserDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchUsersLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupUserDto>> Handle(
        SearchUsersLookupQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.Users.AsNoTracking().Where(user => !user.IsDeleted);

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            query = query.Where(user =>
                user.FullName.ToUpper().Contains(normalized) ||
                user.Email.ToUpper().Contains(normalized) ||
                (user.DocumentNumber != null && user.DocumentNumber.ToUpper().Contains(normalized)));
        }

        var email = request.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email))
        {
            var normalizedEmail = email!.ToUpperInvariant();
            query = query.Where(user => user.Email.ToUpper().Contains(normalizedEmail));
        }

        var roleCode = request.RoleCode?.Trim();
        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            var normalizedRoleCode = roleCode!.ToUpperInvariant();
            query = query.Where(user => user.UserRoles.Any(userRole =>
                userRole.Role.Code == normalizedRoleCode));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(user => user.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(user => user.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new LookupUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Roles = user.UserRoles.Select(userRole => userRole.Role.Code).Distinct().ToList(),
                IsActive = user.IsActive,
                Label = $"{user.FullName} <{user.Email}>",
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupUserDto>(items, pageNumber, pageSize, totalCount);
    }
}

