using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.DeactivateAdminUser;

public sealed class DeactivateAdminUserCommand : IRequest<AdminUserDetailDto?>
{
    public Guid Id { get; init; }
    public Guid ActorUserId { get; init; }
}

