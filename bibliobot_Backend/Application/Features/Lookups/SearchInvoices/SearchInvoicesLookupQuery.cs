using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using MediatR;

namespace Application.Features.Lookups.SearchInvoices;

public sealed class SearchInvoicesLookupQuery : IRequest<PagedResult<LookupInvoiceDto>>
{
    public string? Q { get; init; }
    public string? InvoiceNumber { get; init; }
    public string? CustomerEmail { get; init; }
    public Guid? SaleId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

