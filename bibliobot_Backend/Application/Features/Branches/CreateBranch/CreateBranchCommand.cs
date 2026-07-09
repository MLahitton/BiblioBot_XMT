using Application.Features.Branches.Common;
using MediatR;

namespace Application.Features.Branches.CreateBranch;

public sealed class CreateBranchCommand : IRequest<BranchDto>
{
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
}

