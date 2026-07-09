using MediatR;

namespace Application.Features.Admin.DeleteAdminUser;

public sealed class DeleteAdminUserCommand : IRequest<bool>
{
    public Guid Id { get; init; }
    public Guid ActorUserId { get; init; }
}
