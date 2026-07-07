using Application.Features.Reports.Common;
using MediatR;

namespace Application.Features.Reports.GetLowStockReport;

public sealed class GetLowStockReportQuery : IRequest<IReadOnlyCollection<LowStockBookDto>>
{
    public Guid? BranchId { get; init; }
    public int Limit { get; init; } = 50;
}

