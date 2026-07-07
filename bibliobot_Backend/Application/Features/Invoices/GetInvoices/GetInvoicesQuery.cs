using Application.Common.DTOs;
using Application.Features.Invoices.Common;
using MediatR;

namespace Application.Features.Invoices.GetInvoices;

public sealed class GetInvoicesQuery : IRequest<PagedResult<InvoiceListItemDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? CustomerId { get; init; }
    public Guid? SaleId { get; init; }
    public bool? IsCancelled { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public bool CanReadAll { get; init; }
    public bool CanReadOwn { get; init; }
    public Guid CurrentUserId { get; init; }
}

