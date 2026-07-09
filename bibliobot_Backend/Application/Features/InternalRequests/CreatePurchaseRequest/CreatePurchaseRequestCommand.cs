using Application.Features.InternalRequests.Common;
using MediatR;

namespace Application.Features.InternalRequests.CreatePurchaseRequest;

public sealed class CreatePurchaseRequestCommand : IRequest<InternalRequestDto>
{
    public Guid BranchId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<CreateInternalRequestItemCommand> Items { get; init; } = Array.Empty<CreateInternalRequestItemCommand>();
}

public sealed class CreateInternalRequestItemCommand
{
    public Guid BookId { get; init; }
    public int Quantity { get; init; }
}
