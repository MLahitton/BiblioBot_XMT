using Application.Features.InternalRequests.Common;
using MediatR;

namespace Application.Features.InternalRequests.CreateTransferRequest;

public sealed class CreateTransferRequestCommand : IRequest<InternalRequestDto>
{
    public Guid SourceBranchId { get; init; }
    public Guid DestinationBranchId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<CreateInternalRequestItemCommand> Items { get; init; } = Array.Empty<CreateInternalRequestItemCommand>();
}

public sealed class CreateInternalRequestItemCommand
{
    public Guid BookId { get; init; }
    public int Quantity { get; init; }
}
