using Application.Common.Interfaces;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.DeleteAdminUser;

public sealed class DeleteAdminUserCommandHandler : IRequestHandler<DeleteAdminUserCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteAdminUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteAdminUserCommand request, CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty || request.ActorUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        if (request.Id == request.ActorUserId)
        {
            throw new ArgumentException("No puedes eliminar tu propia cuenta administradora.");
        }

        var actorIsActive = await _context.Users.AnyAsync(
            actor => actor.Id == request.ActorUserId && actor.IsActive && !actor.IsDeleted,
            cancellationToken);

        if (!actorIsActive)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var user = await _context.Users.FirstOrDefaultAsync(
            existing => existing.Id == request.Id && !existing.IsDeleted,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        if (user.IsActive)
        {
            throw new InvalidOperationException("Solo se pueden eliminar usuarios inactivos.");
        }

        var adminRoleId = await _context.Roles
            .Where(role => role.Code == RoleCodes.Admin && role.IsActive)
            .Select(role => role.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (adminRoleId != Guid.Empty)
        {
            var userIsAdmin = await _context.UserRoles.AnyAsync(
                userRole => userRole.UserId == request.Id && userRole.RoleId == adminRoleId,
                cancellationToken);

            if (userIsAdmin)
            {
                var activeAdmins = await _context.Users.AsNoTracking()
                    .Where(activeUser => activeUser.IsActive && !activeUser.IsDeleted)
                    .Where(activeUser => activeUser.UserRoles.Any(userRole => userRole.RoleId == adminRoleId))
                    .CountAsync(cancellationToken);

                if (activeAdmins <= 1)
                {
                    throw new InvalidOperationException("No se puede eliminar al ultimo administrador activo.");
                }
            }
        }

        var now = DateTimeOffset.UtcNow;
        user.IsActive = false;
        user.IsDeleted = true;
        user.DeletedAt = now;
        user.UpdatedAt = now;

        var activeRefreshTokens = await _context.RefreshTokens
            .Where(token => token.UserId == user.Id && token.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeRefreshTokens)
        {
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
