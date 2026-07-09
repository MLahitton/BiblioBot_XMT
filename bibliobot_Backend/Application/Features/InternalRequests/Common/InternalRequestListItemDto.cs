namespace Application.Features.InternalRequests.Common;

public sealed class InternalRequestListItemDto
{
    public Guid Id { get; init; }
    public string RequestTypeCode { get; init; } = string.Empty;
    public string RequestTypeName { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public Guid RequestedByUserId { get; init; }
    public string RequestedByUserName { get; init; } = string.Empty;
    public Guid? SourceBranchId { get; init; }
    public string? SourceBranchName { get; init; }
    public Guid? DestinationBranchId { get; init; }
    public string? DestinationBranchName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public int TotalItems { get; init; }
    public int TotalQuantity { get; init; }
}
