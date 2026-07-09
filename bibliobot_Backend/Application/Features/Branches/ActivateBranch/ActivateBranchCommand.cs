using Application.Features.Branches.Common;
using MediatR;

namespace Application.Features.Branches.ActivateBranch;

public sealed class ActivateBranchCommand : IRequest<BranchDto?>
{
    public Guid Id { get; init; }
}

