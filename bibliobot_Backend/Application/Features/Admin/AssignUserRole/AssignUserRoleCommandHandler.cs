using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using Application.Features.Admin.GetAdminUserById;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.AssignUserRole;

public sealed class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, AdminUserDetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public AssignUserRoleCommandHandler(
        IApplicationDbContext context,
        ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<AdminUserDetailDto?> Handle(
        AssignUserRoleCommand request,
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

        var alreadyHasRole = await _context.UserRoles.AnyAsync(
            userRole => userRole.UserId == request.UserId && userRole.RoleId == role.Id,
            cancellationToken);

        if (!alreadyHasRole)
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = request.UserId,
                RoleId = role.Id,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        return await _sender.Send(
            new GetAdminUserByIdQuery { Id = request.UserId },
            cancellationToken);
    }
}
