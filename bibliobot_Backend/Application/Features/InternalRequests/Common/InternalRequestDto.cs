using System;
using System.Collections.Generic;

namespace Application.Features.InternalRequests.Common;

public sealed class InternalRequestDto
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
    public string? Notes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset? ApprovedAt { get; init; }
    public DateTimeOffset? RejectedAt { get; init; }
    public DateTimeOffset? ExecutedAt { get; init; }
    public IReadOnlyCollection<InternalRequestItemDto> Items { get; init; } = Array.Empty<InternalRequestItemDto>();
}
