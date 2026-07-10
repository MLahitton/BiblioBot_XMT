using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Text;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchAuthors;

public sealed class SearchAuthorsLookupQueryHandler : IRequestHandler<SearchAuthorsLookupQuery, PagedResult<LookupAuthorDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchAuthorsLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupAuthorDto>> Handle(SearchAuthorsLookupQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var normalizedQuery = TextSearchNormalizer.Normalize(request.Q);

        var authors = await _context.Authors
            .AsNoTracking()
            .Where(author => author.IsActive)
            .Select(author => new LookupAuthorDto
            {
                Id = author.Id,
                Label = author.FullName,
                FullName = author.FullName,
                IsActive = author.IsActive
            })
            .ToListAsync(cancellationToken);

        var filteredAuthors = authors
            .Where(author => string.IsNullOrWhiteSpace(normalizedQuery)
                || TextSearchNormalizer.ContainsNormalized(author.FullName, normalizedQuery))
            .OrderBy(author => author.FullName)
            .ToList();

        var totalCount = filteredAuthors.Count;
        var items = filteredAuthors
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<LookupAuthorDto>(items, pageNumber, pageSize, totalCount);
    }
}
