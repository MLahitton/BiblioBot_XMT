using Application.Features.Branches.Common;
using MediatR;

namespace Application.Features.Branches.GetBranchById;

public sealed class GetBranchByIdQuery : IRequest<BranchDto?>
{
    public Guid Id { get; init; }
}

