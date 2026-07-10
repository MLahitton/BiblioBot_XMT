using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Common.Text;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchCategories;

public sealed class SearchCategoriesLookupQueryHandler : IRequestHandler<SearchCategoriesLookupQuery, PagedResult<LookupCategoryDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchCategoriesLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupCategoryDto>> Handle(SearchCategoriesLookupQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = Math.Max(1, request.PageNumber);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var normalizedQuery = TextSearchNormalizer.Normalize(request.Q);

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .Select(category => new LookupCategoryDto
            {
                Id = category.Id,
                Label = category.Name,
                Name = category.Name,
                IsActive = category.IsActive
            })
            .ToListAsync(cancellationToken);

        var filteredCategories = categories
            .Where(category => string.IsNullOrWhiteSpace(normalizedQuery)
                || TextSearchNormalizer.ContainsNormalized(category.Name, normalizedQuery))
            .OrderBy(category => category.Name)
            .ToList();

        var totalCount = filteredCategories.Count;
        var items = filteredCategories
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<LookupCategoryDto>(items, pageNumber, pageSize, totalCount);
    }
}
