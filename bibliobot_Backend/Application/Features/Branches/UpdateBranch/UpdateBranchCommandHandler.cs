using Application.Common.Interfaces;
using Application.Features.Branches.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Branches.UpdateBranch;

public sealed class UpdateBranchCommandHandler : IRequestHandler<UpdateBranchCommand, BranchDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateBranchCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BranchDto?> Handle(
        UpdateBranchCommand request,
        CancellationToken cancellationToken)
    {
        var branch = await _context.Branches.FirstOrDefaultAsync(branch => branch.Id == request.Id, cancellationToken);
        if (branch is null)
        {
            return null;
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("El nombre es obligatorio.");
        }

        if (name.Length > 120)
        {
            throw new ArgumentException("El nombre debe tener máximo 120 caracteres.");
        }

        var address = request.Address?.Trim();
        if (address is not null && address.Length > 250)
        {
            throw new ArgumentException("La dirección debe tener máximo 250 caracteres.");
        }

        var normalizedName = name.ToUpperInvariant();
        var exists = await _context.Branches.AnyAsync(
            current => current.Id != request.Id && current.Name.ToUpper() == normalizedName,
            cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Ya existe una sede con ese nombre.");
        }

        branch.Name = name;
        branch.Address = address;
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

