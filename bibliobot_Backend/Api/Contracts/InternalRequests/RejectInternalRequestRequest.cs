namespace Api.Contracts.InternalRequests;

public sealed class RejectInternalRequestRequest
{
    public string Reason { get; init; } = string.Empty;
}
