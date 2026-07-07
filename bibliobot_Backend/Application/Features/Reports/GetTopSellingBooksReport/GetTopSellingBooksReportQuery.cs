using Application.Features.Reports.Common;
using MediatR;

namespace Application.Features.Reports.GetTopSellingBooksReport;

public sealed class GetTopSellingBooksReportQuery : IRequest<IReadOnlyCollection<TopSellingBookDto>>
{
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public Guid? BranchId { get; init; }
    public int Limit { get; init; } = 10;
}

