namespace Application.Features.Sales.Common;

public sealed class SaleInvoiceDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal Subtotal { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal Total { get; init; }
    public DateTimeOffset IssuedAt { get; init; }
    public bool IsCancelled { get; init; }
}
