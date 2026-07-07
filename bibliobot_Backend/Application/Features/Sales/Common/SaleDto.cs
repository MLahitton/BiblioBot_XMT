using Application.Features.Sales.Common;

namespace Application.Features.Sales.Common;

public sealed class SaleDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string? CustomerName { get; init; }
    public Guid ActorId { get; init; }
    public string? ActorName { get; init; }
    public Guid? BranchId { get; init; }
    public string? BranchName { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string? StatusName { get; init; }
    public string OriginCode { get; init; } = string.Empty;
    public string? OriginName { get; init; }
    public decimal Subtotal { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal Total { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
    public bool IsIdempotent { get; init; }
    public IReadOnlyCollection<SaleDetailDto> Details { get; init; } = [];
    public SaleInvoiceDto? Invoice { get; init; }
}
