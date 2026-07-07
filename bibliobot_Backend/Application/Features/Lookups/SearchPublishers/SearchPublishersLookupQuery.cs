using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using MediatR;

namespace Application.Features.Lookups.SearchPublishers;

public sealed class SearchPublishersLookupQuery : IRequest<PagedResult<LookupPublisherDto>>
{
    public string? Q { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

