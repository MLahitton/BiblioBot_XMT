using Application.Features.Branches.Common;
using MediatR;

namespace Application.Features.Branches.GetBranches;

public sealed class GetBranchesQuery : IRequest<IReadOnlyCollection<BranchDto>>
{
    public bool IncludeInactive { get; init; } = false;
}

