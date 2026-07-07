using Application.Features.Invoices.Common;
using MediatR;

namespace Application.Features.Invoices.GetInvoiceById;

public sealed class GetInvoiceByIdQuery : IRequest<InvoiceDetailDto?>
{
    public Guid Id { get; init; }
    public bool CanReadAll { get; init; }
    public bool CanReadOwn { get; init; }
    public Guid CurrentUserId { get; init; }
}

