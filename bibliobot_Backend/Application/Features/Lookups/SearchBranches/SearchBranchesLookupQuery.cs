using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using MediatR;

namespace Application.Features.Lookups.SearchBranches;

public sealed class SearchBranchesLookupQuery : IRequest<PagedResult<LookupBranchDto>>
{
    public string? Q { get; init; }
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

