using Application.Features.Branches.Common;
using MediatR;

namespace Application.Features.Branches.UpdateBranch;

public sealed class UpdateBranchCommand : IRequest<BranchDto?>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
}

