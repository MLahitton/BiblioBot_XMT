using Application.Common.Interfaces;
using Application.Features.Branches.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Branches.CreateBranch;

public sealed class CreateBranchCommandHandler : IRequestHandler<CreateBranchCommand, BranchDto>
{
    private readonly IApplicationDbContext _context;

    public CreateBranchCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BranchDto> Handle(
        CreateBranchCommand request,
        CancellationToken cancellationToken)
    {
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
        var exists = await _context.Branches.AnyAsync(branch => branch.Name.ToUpper() == normalizedName, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Ya existe una sede con ese nombre.");
        }

        var branch = new Branch
        {
            Name = name,
            Address = address,
            IsActive = true,
        };

        _context.Branches.Add(branch);
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

