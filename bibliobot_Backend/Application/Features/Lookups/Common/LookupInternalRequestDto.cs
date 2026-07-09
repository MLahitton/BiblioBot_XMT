using System;

namespace Application.Features.Lookups.Common;

public sealed class LookupInternalRequestDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string RequestTypeCode { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public string? RequestedByName { get; init; }
    public string? RequestedByEmail { get; init; }
    public string? SourceBranchName { get; init; }
    public string? DestinationBranchName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

