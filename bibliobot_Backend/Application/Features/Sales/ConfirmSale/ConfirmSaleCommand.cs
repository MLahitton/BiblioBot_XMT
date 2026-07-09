using Application.Features.Sales.Common;
using MediatR;

namespace Application.Features.Sales.ConfirmSale;

public sealed class ConfirmSaleCommand : IRequest<SaleDto>
{
    public Guid Id { get; init; }
}

