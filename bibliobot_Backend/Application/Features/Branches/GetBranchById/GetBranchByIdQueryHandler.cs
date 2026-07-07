using Application.Common.Interfaces;
using Application.Features.Branches.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Branches.GetBranchById;

public sealed class GetBranchByIdQueryHandler : IRequestHandler<GetBranchByIdQuery, BranchDto?>
{
    private readonly IApplicationDbContext _context;

    public GetBranchByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BranchDto?> Handle(
        GetBranchByIdQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _context.Branches.AsNoTracking()
            .Where(branch => branch.Id == request.Id)
            .Select(branch => new BranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address,
                IsActive = branch.IsActive,
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }
}

