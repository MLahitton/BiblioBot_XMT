using Application.Common.Interfaces;
using Application.Features.Branches.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Branches.DisableBranch;

public sealed class DisableBranchCommandHandler : IRequestHandler<DisableBranchCommand, BranchDto?>
{
    private readonly IApplicationDbContext _context;

    public DisableBranchCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BranchDto?> Handle(
        DisableBranchCommand request,
        CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(branch => branch.Id == request.Id, cancellationToken);
        if (branch is null)
        {
            return null;
        }

        if (!branch.IsActive)
        {
            return new BranchDto
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address,
                IsActive = branch.IsActive,
            };
        }

        branch.IsActive = false;
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

