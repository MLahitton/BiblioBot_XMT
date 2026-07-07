using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchAuthors;

public sealed class SearchAuthorsLookupQueryHandler
    : IRequestHandler<SearchAuthorsLookupQuery, PagedResult<LookupAuthorDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchAuthorsLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupAuthorDto>> Handle(
        SearchAuthorsLookupQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.Authors.AsNoTracking();

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            query = query.Where(author =>
                author.FullName.ToUpper().Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(author => author.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(author => new LookupAuthorDto
            {
                Id = author.Id,
                FullName = author.FullName,
                Label = author.FullName,
                IsActive = author.IsActive,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupAuthorDto>(items, pageNumber, pageSize, totalCount);
    }
}

