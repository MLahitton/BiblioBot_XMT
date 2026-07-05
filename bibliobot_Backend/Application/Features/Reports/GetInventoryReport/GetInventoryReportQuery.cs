using Application.Features.Reports.Common;
using MediatR;

namespace Application.Features.Reports.GetInventoryReport;

public sealed class GetInventoryReportQuery : IRequest<InventoryReportDto>
{
    public Guid? BranchId { get; init; }
    public Guid? BookId { get; init; }
    public bool? LowStockOnly { get; init; }
}

