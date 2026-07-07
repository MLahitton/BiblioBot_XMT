using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using Application.Features.Admin.GetAdminUserById;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.RemoveUserRole;

public sealed class RemoveUserRoleCommandHandler : IRequestHandler<RemoveUserRoleCommand, AdminUserDetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public RemoveUserRoleCommandHandler(
        IApplicationDbContext context,
        ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<AdminUserDetailDto?> Handle(
        RemoveUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || request.ActorUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var normalizedRoleCode = (request.RoleCode?.Trim() ?? string.Empty).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedRoleCode))
        {
            throw new ArgumentException("El código de rol es obligatorio.");
        }

        var actorIsActive = await _context.Users.AnyAsync(
            actor => actor.Id == request.ActorUserId && actor.IsActive && !actor.IsDeleted,
            cancellationToken);

        if (!actorIsActive)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(
            user => user.Id == request.UserId && !user.IsDeleted,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var role = await _context.Roles.FirstOrDefaultAsync(
            role => role.Code == normalizedRoleCode,
            cancellationToken);

        if (role is null)
        {
            throw new KeyNotFoundException($"Rol '{normalizedRoleCode}' no encontrado.");
        }

        var userRole = await _context.UserRoles.FirstOrDefaultAsync(
            userRole => userRole.UserId == request.UserId && userRole.RoleId == role.Id,
            cancellationToken);

        if (userRole is null)
        {
            return await _sender.Send(
                new GetAdminUserByIdQuery { Id = request.UserId },
                cancellationToken);
        }

        if (role.Code == RoleCodes.Admin && request.UserId == request.ActorUserId)
        {
            throw new ArgumentException("No puedes quitarte a ti mismo el rol ADMIN.");
        }

        if (role.Code == RoleCodes.Admin)
        {
            var activeAdmins = await _context.Users.AsNoTracking()
                .Where(activeUser => activeUser.IsActive && !activeUser.IsDeleted)
                .Where(activeUser => activeUser.UserRoles.Any(userRole => userRole.RoleId == role.Id))
                .CountAsync(cancellationToken);

            if (activeAdmins <= 1)
            {
                throw new InvalidOperationException("No se puede remover el último ADMIN activo del sistema.");
            }
        }

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        return await _sender.Send(
            new GetAdminUserByIdQuery { Id = request.UserId },
            cancellationToken);
    }
}
