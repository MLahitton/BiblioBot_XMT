using Application.Common.Interfaces;
using Application.Features.Catalog.Publishers.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Catalog.Publishers.GetPublishers;

public sealed class GetPublishersQueryHandler : IRequestHandler<GetPublishersQuery, IReadOnlyCollection<PublisherDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPublishersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<PublisherDto>> Handle(
        GetPublishersQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Publishers.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(publisher => publisher.IsActive);
        }

        return await query
            .OrderBy(publisher => publisher.Name)
            .Select(publisher => new PublisherDto
            {
                Id = publisher.Id,
                Name = publisher.Name,
                IsActive = publisher.IsActive,
            })
            .ToListAsync(cancellationToken);
    }
}

