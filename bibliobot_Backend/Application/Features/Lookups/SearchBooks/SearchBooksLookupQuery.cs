using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using MediatR;

namespace Application.Features.Lookups.SearchBooks;

public sealed class SearchBooksLookupQuery : IRequest<PagedResult<LookupBookDto>>
{
    public string? Q { get; init; }
    public string? Isbn { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

