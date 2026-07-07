using Api.Contracts.InternalRequests;

namespace Api.Contracts.InternalRequests;

public sealed class CreatePurchaseRequestRequest
{
    public Guid BranchId { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyCollection<InternalRequestItemRequest> Items { get; init; } = Array.Empty<InternalRequestItemRequest>();
}
