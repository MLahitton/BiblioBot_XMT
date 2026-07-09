using Application.Common.Interfaces;
using Application.Features.Catalog.Authors.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Authors.CreateAuthor;

public sealed class CreateAuthorCommandHandler : IRequestHandler<CreateAuthorCommand, AuthorDto>
{
    private readonly IApplicationDbContext _context;

    public CreateAuthorCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuthorDto> Handle(
        CreateAuthorCommand request,
        CancellationToken cancellationToken)
    {
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
            author => author.FullName.ToUpper() == normalizedFullName,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Ya existe un autor con ese nombre.");
        }

        var author = new Author
        {
            FullName = fullName,
            IsActive = true,
        };

        _context.Authors.Add(author);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthorDto
        {
            Id = author.Id,
            FullName = author.FullName,
            IsActive = author.IsActive,
        };
    }
}

