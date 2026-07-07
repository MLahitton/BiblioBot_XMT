using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using MediatR;

namespace Application.Features.Lookups.SearchInternalRequests;

public sealed class SearchInternalRequestsLookupQuery : IRequest<PagedResult<LookupInternalRequestDto>>
{
    public string? Q { get; init; }
    public string? RequestTypeCode { get; init; }
    public string? StatusCode { get; init; }
    public string? RequestedByEmail { get; init; }
    public string? BranchName { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

