using Application.Common.Interfaces;
using Application.Features.Branches.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Branches.ActivateBranch;

public sealed class ActivateBranchCommandHandler : IRequestHandler<ActivateBranchCommand, BranchDto?>
{
    private readonly IApplicationDbContext _context;

    public ActivateBranchCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BranchDto?> Handle(
        ActivateBranchCommand request,
        CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(branch => branch.Id == request.Id, cancellationToken);
        if (branch is null)
        {
            return null;
        }

        if (branch.IsActive)
        {
            return new BranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address,
                IsActive = branch.IsActive,
            };
        }

        branch.IsActive = true;
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new BranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Address = branch.Address,
            IsActive = branch.IsActive,
        };
    }
}

