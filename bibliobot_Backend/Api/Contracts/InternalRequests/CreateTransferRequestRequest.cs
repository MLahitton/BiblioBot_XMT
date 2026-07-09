using Api.Contracts.InternalRequests;

namespace Api.Contracts.InternalRequests;

public sealed class CreateTransferRequestRequest
{
    public Guid SourceBranchId { get; init; }
    public Guid DestinationBranchId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<InternalRequestItemRequest> Items { get; init; } = Array.Empty<InternalRequestItemRequest>();
}
