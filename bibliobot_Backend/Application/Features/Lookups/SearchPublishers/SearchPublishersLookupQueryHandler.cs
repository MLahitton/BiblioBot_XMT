using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchPublishers;

public sealed class SearchPublishersLookupQueryHandler
    : IRequestHandler<SearchPublishersLookupQuery, PagedResult<LookupPublisherDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchPublishersLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupPublisherDto>> Handle(
        SearchPublishersLookupQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.Publishers.AsNoTracking();

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            query = query.Where(publisher => publisher.Name.ToUpper().Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(publisher => publisher.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(publisher => new LookupPublisherDto
            {
                Id = publisher.Id,
                Name = publisher.Name,
                Label = publisher.Name,
                IsActive = publisher.IsActive,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupPublisherDto>(items, pageNumber, pageSize, totalCount);
    }
}

