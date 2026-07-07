using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.AssignUserRole;

public sealed class AssignUserRoleCommand : IRequest<AdminUserDetailDto?>
{
    public Guid UserId { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public Guid ActorUserId { get; init; }
}

