using Application.Common.Interfaces;
using Application.Features.Branches.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Branches.GetBranches;

public sealed class GetBranchesQueryHandler : IRequestHandler<GetBranchesQuery, IReadOnlyCollection<BranchDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBranchesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<BranchDto>> Handle(
        GetBranchesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Branches.AsNoTracking();

        if (!request.IncludeInactive)
        {
            query = query.Where(branch => branch.IsActive);
        }

        return await query
            .OrderBy(branch => branch.Name)
            .Select(branch => new BranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address,
                IsActive = branch.IsActive,
            })
            .ToListAsync(cancellationToken);
    }
}

