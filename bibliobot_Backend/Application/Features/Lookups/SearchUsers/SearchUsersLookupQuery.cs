using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using MediatR;

namespace Application.Features.Lookups.SearchUsers;

public sealed class SearchUsersLookupQuery : IRequest<PagedResult<LookupUserDto>>
{
    public string? Q { get; init; }
    public string? Email { get; init; }
    public string? RoleCode { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

