using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Text;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchPublishers;

public sealed class SearchPublishersLookupQueryHandler : IRequestHandler<SearchPublishersLookupQuery, PagedResult<LookupPublisherDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchPublishersLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupPublisherDto>> Handle(SearchPublishersLookupQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var normalizedQuery = TextSearchNormalizer.Normalize(request.Q);

        var publishers = await _context.Publishers
            .AsNoTracking()
            .Where(publisher => publisher.IsActive)
            .Select(publisher => new LookupPublisherDto
            {
                Id = publisher.Id,
                Label = publisher.Name,
                Name = publisher.Name,
                IsActive = publisher.IsActive
            })
            .ToListAsync(cancellationToken);

        var filteredPublishers = publishers
            .Where(publisher => string.IsNullOrWhiteSpace(normalizedQuery)
                || TextSearchNormalizer.ContainsNormalized(publisher.Name, normalizedQuery))
            .OrderBy(publisher => publisher.Name)
            .ToList();

        var totalCount = filteredPublishers.Count;
        var items = filteredPublishers
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<LookupPublisherDto>(items, pageNumber, pageSize, totalCount);
    }
}
