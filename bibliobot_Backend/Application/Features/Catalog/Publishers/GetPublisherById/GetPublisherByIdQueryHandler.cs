using Application.Common.Interfaces;
using Application.Features.Catalog.Publishers.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Publishers.GetPublisherById;

public sealed class GetPublisherByIdQueryHandler : IRequestHandler<GetPublisherByIdQuery, PublisherDto?>
{
    private readonly IApplicationDbContext _context;

    public GetPublisherByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PublisherDto?> Handle(
        GetPublisherByIdQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _context.Publishers.AsNoTracking()
            .Where(publisher => publisher.Id == request.Id)
            .Select(publisher => new PublisherDto
            {
                Id = publisher.Id,
                Name = publisher.Name,
                IsActive = publisher.IsActive,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }
}

