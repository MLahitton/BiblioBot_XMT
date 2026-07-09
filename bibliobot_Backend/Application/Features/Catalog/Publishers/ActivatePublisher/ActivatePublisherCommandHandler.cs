using Application.Common.Interfaces;
using Application.Features.Catalog.Publishers.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Publishers.ActivatePublisher;

public sealed class ActivatePublisherCommandHandler : IRequestHandler<ActivatePublisherCommand, PublisherDto?>
{
    private readonly IApplicationDbContext _context;

    public ActivatePublisherCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublisherDto?> Handle(
        ActivatePublisherCommand request,
        CancellationToken cancellationToken)
    {
        var publisher = await _context.Publishers.FirstOrDefaultAsync(publisher => publisher.Id == request.Id, cancellationToken);

        if (publisher is null)
        {
            return null;
        }

        if (publisher.IsActive)
        {
            return new PublisherDto
            {
                Id = publisher.Id,
                Name = publisher.Name,
                IsActive = publisher.IsActive,
            };
        }

        publisher.IsActive = true;
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

