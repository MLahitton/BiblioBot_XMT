using Application.Features.Branches.Common;
using MediatR;

namespace Application.Features.Branches.DisableBranch;

public sealed class DisableBranchCommand : IRequest<BranchDto?>
{
    public Guid Id { get; init; }
}

