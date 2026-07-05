using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.ActivateAdminUser;

public sealed class ActivateAdminUserCommand : IRequest<AdminUserDetailDto?>
{
    public Guid Id { get; init; }
    public Guid ActorUserId { get; init; }
}

