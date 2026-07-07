using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchCategories;

public sealed class SearchCategoriesLookupQueryHandler
    : IRequestHandler<SearchCategoriesLookupQuery, PagedResult<LookupCategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchCategoriesLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupCategoryDto>> Handle(
        SearchCategoriesLookupQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.Categories.AsNoTracking();

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            query = query.Where(category => category.Name.ToUpper().Contains(normalized));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(category => category.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(category => new LookupCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Label = category.Name,
                IsActive = category.IsActive,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupCategoryDto>(items, pageNumber, pageSize, totalCount);
    }
}

