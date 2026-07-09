using System.Net.Mail;

using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using Application.Features.Admin.GetAdminUserById;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.CreateAdminUser;

public sealed class CreateAdminUserCommandHandler : IRequestHandler<CreateAdminUserCommand, AdminUserDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISender _sender;

    public CreateAdminUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ISender sender)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _sender = sender;
    }

    public async Task<AdminUserDetailDto> Handle(
        CreateAdminUserCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var fullName = request.FullName?.Trim() ?? string.Empty;
        var email = request.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        var password = request.Password ?? string.Empty;
        var phone = request.Phone?.Trim();
        var documentNumber = request.DocumentNumber?.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 160)
        {
            throw new ArgumentException("El nombre completo es obligatorio y debe tener máximo 160 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(email) || email.Length > 180)
        {
            throw new ArgumentException("El correo es obligatorio y debe tener máximo 180 caracteres.");
        }

        if (!IsValidEmail(email))
        {
            throw new ArgumentException("El formato del correo no es válido.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
        }

        if (phone is not null && phone.Length > 40)
        {
            throw new ArgumentException("El telefono debe tener maximo 40 caracteres.");
        }

        if (documentNumber is not null && documentNumber.Length > 50)
        {
            throw new ArgumentException("El documento debe tener maximo 50 caracteres.");
        }

        var actorIsActive = await _context.Users.AnyAsync(
            actor => actor.Id == request.ActorUserId && actor.IsActive && !actor.IsDeleted,
            cancellationToken);

        if (!actorIsActive)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var emailExists = await _context.Users.AnyAsync(
            user => user.Email == email,
            cancellationToken);

        if (emailExists)
        {
            throw new InvalidOperationException("EMAIL_ALREADY_EXISTS");
        }

        if (request.RoleCodes is null || request.RoleCodes.Count == 0)
        {
            throw new ArgumentException("Debe asignar al menos un rol.");
        }

        var normalizedRoleCodes = request.RoleCodes
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToUpperInvariant())
            .Where(role => role.Length > 0)
            .ToList();

        if (normalizedRoleCodes.Count == 0)
        {
            throw new ArgumentException("Debe asignar al menos un rol válido.");
        }

        var duplicatedRoles = normalizedRoleCodes
            .GroupBy(role => role)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicatedRoles.Count > 0)
        {
            throw new ArgumentException($"No se permiten roles duplicados: {string.Join(", ", duplicatedRoles)}.");
        }

        var distinctRoleCodes = normalizedRoleCodes
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (distinctRoleCodes.Contains(RoleCodes.Admin))
        {
            throw new ArgumentException("Solo la cuenta admin@gmail.com puede tener rol ADMIN.");
        }

        var roles = await _context.Roles
            .Where(role => distinctRoleCodes.Contains(role.Code))
            .OrderBy(role => role.Code)
            .ToListAsync(cancellationToken);

        if (roles.Count != distinctRoleCodes.Count)
        {
            var existingRoleCodes = roles.Select(role => role.Code);
            var missingRoleCodes = distinctRoleCodes
                .Except(existingRoleCodes)
                .ToList();

            throw new KeyNotFoundException($"Rol no encontrado: {string.Join(", ", missingRoleCodes)}");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = string.Empty,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
            DocumentNumber = string.IsNullOrWhiteSpace(documentNumber) ? null : documentNumber,
            IsActive = true,
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _context.Users.Add(user);

        _context.UserRoles.AddRange(
            roles.Select(role => new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                CreatedAt = now,
            }));

        await _context.SaveChangesAsync(cancellationToken);

        var detail = await _sender.Send(
            new GetAdminUserByIdQuery { Id = user.Id },
            cancellationToken);

        return detail ?? throw new InvalidOperationException("No fue posible obtener el usuario creado.");
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
