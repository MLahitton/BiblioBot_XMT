using Application.Features.Sales.Common;
using MediatR;

namespace Application.Features.Sales.GetSaleById;

public sealed class GetSaleByIdQuery : IRequest<SaleDto?>
{
    public Guid Id { get; init; }
    public bool CanReadAll { get; init; }
    public Guid ActorId { get; init; }
}

