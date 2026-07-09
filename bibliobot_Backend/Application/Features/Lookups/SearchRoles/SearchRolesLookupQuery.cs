using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using MediatR;

namespace Application.Features.Lookups.SearchRoles;

public sealed class SearchRolesLookupQuery : IRequest<PagedResult<LookupRoleDto>>
{
    public string? Q { get; init; }
    public string? Code { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

