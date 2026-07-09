using Application.Common.Interfaces;
using Application.Features.Catalog.Publishers.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Publishers.UpdatePublisher;

public sealed class UpdatePublisherCommandHandler : IRequestHandler<UpdatePublisherCommand, PublisherDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdatePublisherCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublisherDto?> Handle(
        UpdatePublisherCommand request,
        CancellationToken cancellationToken)
    {
        var publisher = await _context.Publishers.FirstOrDefaultAsync(publisher => publisher.Id == request.Id, cancellationToken);

        if (publisher is null)
        {
            return null;
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre es obligatorio.");
        }

        if (name.Length > 160)
        {
            throw new ArgumentException("El nombre debe tener máximo 160 caracteres.");
        }

        var normalizedName = name.ToUpperInvariant();
        var exists = await _context.Publishers.AnyAsync(
            current => current.Id != request.Id && current.Name.ToUpper() == normalizedName,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Ya existe una editorial con ese nombre.");
        }

        publisher.Name = name;
        publisher.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new PublisherDto
        {
            Id = publisher.Id,
            Name = publisher.Name,
            IsActive = publisher.IsActive,
        };
    }
}

