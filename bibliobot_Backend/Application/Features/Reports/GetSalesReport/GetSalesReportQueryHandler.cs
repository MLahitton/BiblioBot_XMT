using Application.Common.Interfaces;
using Application.Features.Reports.Common;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Reports.GetSalesReport;

public sealed class GetSalesReportQueryHandler : IRequestHandler<GetSalesReportQuery, SalesReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetSalesReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SalesReportDto> Handle(GetSalesReportQuery request, CancellationToken cancellationToken)
    {
        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            throw new ArgumentException("El rango de fechas es inválido. 'from' debe ser menor o igual a 'to'.");
        }

        var salesQuery = _context.Sales.AsNoTracking()
            .Include(sale => sale.Status)
            .Include(sale => sale.Origin)
            .Include(sale => sale.Branch)
            .AsQueryable();

        if (request.From.HasValue)
        {
            salesQuery = salesQuery.Where(sale => sale.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            salesQuery = salesQuery.Where(sale => sale.CreatedAt <= request.To.Value);
        }

        if (request.BranchId.HasValue)
        {
            salesQuery = salesQuery.Where(sale => sale.BranchId == request.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.OriginCode))
        {
            var originCode = request.OriginCode.Trim().ToUpperInvariant();
            salesQuery = salesQuery.Where(sale => sale.Origin.Code == originCode);
        }

        if (!string.IsNullOrWhiteSpace(request.StatusCode))
        {
            var statusCode = request.StatusCode.Trim().ToUpperInvariant();
            salesQuery = salesQuery.Where(sale => sale.Status.Code == statusCode);
        }

        var totals = await salesQuery
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalSalesCount = group.Count(),
                ConfirmedSalesCount = group.Count(sale => sale.Status.Code == SaleStatusCodes.Confirmed),
                PendingSalesCount = group.Count(sale => sale.Status.Code == SaleStatusCodes.PendingConfirmation),
                CancelledSalesCount = group.Count(sale => sale.Status.Code == SaleStatusCodes.Cancelled),
                RejectedSalesCount = group.Count(sale => sale.Status.Code == SaleStatusCodes.Rejected),
                SubtotalAmount = group.Sum(sale => sale.Subtotal),
                TaxAmount = group.Sum(sale => sale.TaxTotal),
                TotalRevenue = group.Sum(sale => sale.Total),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var totalItemsSold = await salesQuery
            .SelectMany(sale => sale.SaleDetails)
            .SumAsync(detail => detail.Quantity, cancellationToken);

        var normalizedStatusCode = string.IsNullOrWhiteSpace(request.StatusCode)
            ? null
            : request.StatusCode.Trim().ToUpperInvariant();

        var normalizedOriginCode = string.IsNullOrWhiteSpace(request.OriginCode)
            ? null
            : request.OriginCode.Trim().ToUpperInvariant();

        var branchName = request.BranchId.HasValue
            ? await _context.Branches.AsNoTracking()
                .Where(branch => branch.Id == request.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (totals is null)
        {
            return new SalesReportDto
            {
                From = request.From,
                To = request.To,
                BranchId = request.BranchId,
                BranchName = branchName,
                StatusCode = normalizedStatusCode,
                OriginCode = normalizedOriginCode,
                TotalSalesCount = 0,
                ConfirmedSalesCount = 0,
                PendingSalesCount = 0,
                CancelledSalesCount = 0,
                RejectedSalesCount = 0,
                TotalItemsSold = 0,
                SubtotalAmount = 0,
                TaxAmount = 0,
                TotalRevenue = 0,
                AverageTicket = 0,
            };
        }

        var averageTicket = totals.TotalSalesCount == 0
            ? 0m
            : totals.TotalRevenue / totals.TotalSalesCount;

        return new SalesReportDto
        {
            From = request.From,
            To = request.To,
            BranchId = request.BranchId,
            BranchName = branchName,
            StatusCode = normalizedStatusCode,
            OriginCode = normalizedOriginCode,
            TotalSalesCount = totals.TotalSalesCount,
            ConfirmedSalesCount = totals.ConfirmedSalesCount,
            PendingSalesCount = totals.PendingSalesCount,
            CancelledSalesCount = totals.CancelledSalesCount,
            RejectedSalesCount = totals.RejectedSalesCount,
            TotalItemsSold = totalItemsSold,
            SubtotalAmount = totals.SubtotalAmount,
            TaxAmount = totals.TaxAmount,
            TotalRevenue = totals.TotalRevenue,
            AverageTicket = averageTicket,
        };
    }
}

