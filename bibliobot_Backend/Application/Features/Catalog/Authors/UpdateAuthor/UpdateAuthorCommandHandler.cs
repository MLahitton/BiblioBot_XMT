using Application.Common.Interfaces;
using Application.Features.Catalog.Authors.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Authors.UpdateAuthor;

public sealed class UpdateAuthorCommandHandler : IRequestHandler<UpdateAuthorCommand, AuthorDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateAuthorCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorDto?> Handle(
        UpdateAuthorCommand request,
        CancellationToken cancellationToken)
    {
        var author = await _context.Authors.FirstOrDefaultAsync(author => author.Id == request.Id, cancellationToken);

        if (author is null)
        {
            return null;
        }

        var fullName = request.FullName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("El nombre completo es obligatorio.");
        }

        if (fullName.Length > 160)
        {
            throw new ArgumentException("El nombre completo debe tener máximo 160 caracteres.");
        }

        var normalizedFullName = fullName.ToUpperInvariant();
        var exists = await _context.Authors.AnyAsync(
            current => current.Id != request.Id && current.FullName.ToUpper() == normalizedFullName,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Ya existe un autor con ese nombre.");
        }

        author.FullName = fullName;
        author.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new AuthorDto
        {
            Id = author.Id,
            FullName = author.FullName,
            IsActive = author.IsActive,
        };
    }
}

