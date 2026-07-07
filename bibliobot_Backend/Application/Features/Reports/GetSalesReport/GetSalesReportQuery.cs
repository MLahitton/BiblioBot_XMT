using Application.Features.Reports.Common;
using MediatR;

namespace Application.Features.Reports.GetSalesReport;

public sealed class GetSalesReportQuery : IRequest<SalesReportDto>
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public Guid? BranchId { get; init; }
    public string? OriginCode { get; init; }
    public string? StatusCode { get; init; }
}
