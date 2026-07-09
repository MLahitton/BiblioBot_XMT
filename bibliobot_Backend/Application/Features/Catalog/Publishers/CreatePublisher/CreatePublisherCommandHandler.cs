using Application.Common.Interfaces;
using Application.Features.Catalog.Publishers.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Publishers.CreatePublisher;

public sealed class CreatePublisherCommandHandler : IRequestHandler<CreatePublisherCommand, PublisherDto>
{
    private readonly IApplicationDbContext _context;

    public CreatePublisherCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublisherDto> Handle(
        CreatePublisherCommand request,
        CancellationToken cancellationToken)
    {
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
            publisher => publisher.Name.ToUpper() == normalizedName,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Ya existe una editorial con ese nombre.");
        }

        var publisher = new Publisher
        {
            Name = name,
            IsActive = true,
        };

        _context.Publishers.Add(publisher);
        await _context.SaveChangesAsync(cancellationToken);

        return new PublisherDto
        {
            Id = publisher.Id,
            Name = publisher.Name,
            IsActive = publisher.IsActive,
        };
    }
}

