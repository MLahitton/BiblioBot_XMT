using System;

namespace Application.Features.Lookups.Common;

public sealed class LookupInvoiceDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid SaleId { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
    public decimal Total { get; init; }
    public DateTimeOffset IssuedAt { get; init; }
    public bool IsCancelled { get; init; }
}

