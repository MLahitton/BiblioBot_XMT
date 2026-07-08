using System.Net.Mail;
using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using Application.Features.Admin.GetAdminUserById;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.UpdateAdminUser;

public sealed class UpdateAdminUserCommandHandler : IRequestHandler<UpdateAdminUserCommand, AdminUserDetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;

    public UpdateAdminUserCommandHandler(
        IApplicationDbContext context,
        ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<AdminUserDetailDto?> Handle(
        UpdateAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id == Guid.Empty || request.ActorUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var actorIsActive = await _context.Users.AnyAsync(
            actor => actor.Id == request.ActorUserId && actor.IsActive && !actor.IsDeleted,
            cancellationToken);

        if (!actorIsActive)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var user = await _context.Users
            .Include(existing => existing.UserRoles)
            .FirstOrDefaultAsync(
                existing => existing.Id == request.Id && !existing.IsDeleted,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        if (user.Id == request.ActorUserId)
        {
            throw new ArgumentException("No puedes editar tu propia cuenta administradora.");
        }

        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var phone = request.Phone?.Trim();
        var documentNumber = request.DocumentNumber?.Trim();

        ValidateUserData(fullName, email, phone, documentNumber);

        var emailExists = await _context.Users.AnyAsync(
            existing => existing.Id != user.Id && existing.Email == email,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("EMAIL_ALREADY_EXISTS");
        }

        user.FullName = fullName;
        user.Email = email;
        user.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
        user.DocumentNumber = string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (request.RoleCodes is not null)
        {
            var roles = await GetRolesAsync(request.RoleCodes, cancellationToken);
            ApplyRoles(user, roles);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _sender.Send(
            new GetAdminUserByIdQuery { Id = user.Id },
            cancellationToken);
    }

    private static void ValidateUserData(
        string fullName,
        string email,
        string? phone,
        string? documentNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 150)
        {
            throw new ArgumentException("El nombre completo es obligatorio y debe tener maximo 150 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(email) || email.Length > 180)
        {
            throw new ArgumentException("El correo es obligatorio y debe tener maximo 180 caracteres.");
        }

        if (!IsValidEmail(email))
        {
            throw new ArgumentException("El formato del correo no es valido.");
        }

        if (phone is not null && phone.Length > 40)
        {
            throw new ArgumentException("El telefono debe tener maximo 40 caracteres.");
        }

        if (documentNumber is not null && documentNumber.Length > 50)
        {
            throw new ArgumentException("El documento debe tener maximo 50 caracteres.");
        }
    }

    private async Task<List<Role>> GetRolesAsync(
        IReadOnlyCollection<string> roleCodes,
        CancellationToken cancellationToken)
    {
        if (roleCodes.Count == 0)
        {
            throw new ArgumentException("Debe asignar al menos un rol.");
        }

        var normalizedRoleCodes = roleCodes
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedRoleCodes.Count == 0)
        {
            throw new ArgumentException("Debe asignar al menos un rol valido.");
        }

        if (normalizedRoleCodes.Contains(RoleCodes.Admin))
        {
            throw new ArgumentException("Solo la cuenta admin@gmail.com puede tener rol ADMIN.");
        }

        var roles = await _context.Roles
            .Where(role => normalizedRoleCodes.Contains(role.Code) && role.IsActive)
            .OrderBy(role => role.Code)
            .ToListAsync(cancellationToken);

        if (roles.Count != normalizedRoleCodes.Count)
        {
            var missingRoleCodes = normalizedRoleCodes
                .Except(roles.Select(role => role.Code))
                .ToList();

            throw new KeyNotFoundException($"Rol no encontrado: {string.Join(", ", missingRoleCodes)}");
        }

        return roles;
    }

    private void ApplyRoles(User user, IReadOnlyCollection<Role> roles)
    {
        var desiredRoleIds = roles.Select(role => role.Id).ToHashSet();
        var rolesToRemove = user.UserRoles
            .Where(userRole => !desiredRoleIds.Contains(userRole.RoleId))
            .ToList();

        _context.UserRoles.RemoveRange(rolesToRemove);

        var currentRoleIds = user.UserRoles
            .Where(userRole => desiredRoleIds.Contains(userRole.RoleId))
            .Select(userRole => userRole.RoleId)
            .ToHashSet();

        foreach (var role in roles.Where(role => !currentRoleIds.Contains(role.Id)))
        {
            _context.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
