using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using MediatR;

namespace Application.Features.Lookups.SearchCategories;

public sealed class SearchCategoriesLookupQuery : IRequest<PagedResult<LookupCategoryDto>>
{
    public string? Q { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

