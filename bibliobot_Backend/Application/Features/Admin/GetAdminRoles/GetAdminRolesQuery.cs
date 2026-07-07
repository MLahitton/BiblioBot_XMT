using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.GetAdminRoles;

public sealed class GetAdminRolesQuery : IRequest<IReadOnlyCollection<AdminRoleDto>>
{
}

