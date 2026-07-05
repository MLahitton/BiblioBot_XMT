using System;

namespace Application.Features.Invoices.Common;

public sealed class InvoiceSaleDetailDto
{
    public Guid SaleId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string? StatusName { get; init; }
    public string OriginCode { get; init; } = string.Empty;
    public string? OriginName { get; init; }
    public Guid? BranchId { get; init; }
    public string? BranchName { get; init; }
    public DateTimeOffset SaleCreatedAt { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
}

