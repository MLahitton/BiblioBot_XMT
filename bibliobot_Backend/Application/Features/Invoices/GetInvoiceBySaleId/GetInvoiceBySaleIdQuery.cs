using Application.Features.Invoices.Common;
using MediatR;

namespace Application.Features.Invoices.GetInvoiceBySaleId;

public sealed class GetInvoiceBySaleIdQuery : IRequest<InvoiceDetailDto?>
{
    public Guid SaleId { get; init; }
    public bool CanReadAll { get; init; }
    public bool CanReadOwn { get; init; }
    public Guid CurrentUserId { get; init; }
}

