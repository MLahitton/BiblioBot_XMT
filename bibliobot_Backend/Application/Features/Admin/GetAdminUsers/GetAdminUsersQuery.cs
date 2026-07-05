using Application.Common.DTOs;
using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.GetAdminUsers;

public sealed class GetAdminUsersQuery : IRequest<PagedResult<AdminUserListItemDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? RoleCode { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsEmailConfirmed { get; init; }
}

