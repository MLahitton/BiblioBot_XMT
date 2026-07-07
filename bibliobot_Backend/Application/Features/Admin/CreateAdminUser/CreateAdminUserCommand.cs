using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.CreateAdminUser;

public sealed class CreateAdminUserCommand : IRequest<AdminUserDetailDto>
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public IReadOnlyCollection<string> RoleCodes { get; init; } = [];
    public Guid ActorUserId { get; init; }
}
