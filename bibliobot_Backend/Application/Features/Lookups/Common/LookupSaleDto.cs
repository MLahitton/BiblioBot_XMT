using System;

namespace Application.Features.Lookups.Common;

public sealed class LookupSaleDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string OriginCode { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
}

