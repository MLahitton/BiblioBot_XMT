using System;
using System.Collections.Generic;

namespace Application.Features.Invoices.Common;

public sealed class InvoiceDetailDto
{
    public Guid Id { get; init; }
    public Guid SaleId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public decimal Subtotal { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal Total { get; init; }
    public DateTimeOffset IssuedAt { get; init; }
    public bool IsCancelled { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public InvoiceSaleDetailDto Sale { get; init; } = null!;
    public IReadOnlyCollection<InvoiceBookItemDto> Items { get; init; } = Array.Empty<InvoiceBookItemDto>();
}

