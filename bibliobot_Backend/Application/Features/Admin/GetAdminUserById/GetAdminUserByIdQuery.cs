using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.GetAdminUserById;

public sealed class GetAdminUserByIdQuery : IRequest<AdminUserDetailDto?>
{
    public Guid Id { get; init; }
}

