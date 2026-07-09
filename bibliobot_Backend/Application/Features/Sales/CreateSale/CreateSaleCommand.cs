using Application.Features.Sales.Common;
using MediatR;

namespace Application.Features.Sales.CreateSale;

public sealed class CreateSaleCommand : IRequest<SaleDto>
{
    public string SessionId { get; init; } = string.Empty;
    public Guid? BranchId { get; init; }
    public string? OriginCode { get; init; }
}

