using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.GetAdminPermissions;

public sealed class GetAdminPermissionsQuery : IRequest<IReadOnlyCollection<AdminPermissionDto>>
{
}

