namespace Application.Features.Reports.Common;

public sealed class SalesReportDto
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public Guid? BranchId { get; init; }
    public string? BranchName { get; init; }
    public string? StatusCode { get; init; }
    public string? OriginCode { get; init; }
    public int TotalSalesCount { get; init; }
    public int ConfirmedSalesCount { get; init; }
    public int PendingSalesCount { get; init; }
    public int CancelledSalesCount { get; init; }
    public int RejectedSalesCount { get; init; }
    public int TotalItemsSold { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageTicket { get; init; }
}

