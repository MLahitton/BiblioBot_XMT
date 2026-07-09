using Domain.Common;

namespace Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsCancelled { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}
