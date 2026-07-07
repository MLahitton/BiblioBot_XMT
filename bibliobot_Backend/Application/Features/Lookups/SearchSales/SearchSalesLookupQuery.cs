using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using MediatR;

namespace Application.Features.Lookups.SearchSales;

public sealed class SearchSalesLookupQuery : IRequest<PagedResult<LookupSaleDto>>
{
    public string? Q { get; init; }
    public string? CustomerEmail { get; init; }
    public string? StatusCode { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

