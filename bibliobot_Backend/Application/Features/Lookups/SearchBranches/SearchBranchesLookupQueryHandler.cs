using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchBranches;

public sealed class SearchBranchesLookupQueryHandler
    : IRequestHandler<SearchBranchesLookupQuery, PagedResult<LookupBranchDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchBranchesLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupBranchDto>> Handle(
        SearchBranchesLookupQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.Branches.AsNoTracking();

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            query = query.Where(branch =>
                branch.Name.ToUpper().Contains(normalized) ||
                (branch.Address != null && branch.Address.ToUpper().Contains(normalized)));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(branch => branch.IsActive == request.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(branch => branch.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(branch => new LookupBranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address,
                City = null,
                Label = $"{branch.Name} - {branch.Address}",
                IsActive = branch.IsActive,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupBranchDto>(items, pageNumber, pageSize, totalCount);
    }
}

