using Application.Common.DTOs;
using Application.Features.Sales.Common;
using MediatR;

namespace Application.Features.Sales.GetSales;

public sealed class GetSalesQuery : IRequest<PagedResult<SaleDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? StatusCode { get; init; }
    public string? OriginCode { get; init; }
    public Guid? CustomerId { get; init; }
    public bool CanReadAll { get; init; }
    public Guid ActorId { get; init; }
}

