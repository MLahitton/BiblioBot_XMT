using Application.Features.Reports.GetInventoryReport;
using Application.Features.Reports.GetLowStockReport;
using Application.Features.Reports.GetSalesReport;
using Application.Features.Reports.GetTopSellingBooksReport;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/reportes")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISender _sender;

    public ReportsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("ventas")]
    [Authorize(Policy = PermissionCodes.ReportsSalesRead)]
    public async Task<IActionResult> GetSalesReport(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] string? originCode = null,
        [FromQuery] string? statusCode = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new GetSalesReportQuery
                {
                    From = from,
                    To = to,
                    BranchId = branchId,
                    OriginCode = originCode,
                    StatusCode = statusCode,
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("inventario")]
    [Authorize(Policy = PermissionCodes.ReportsInventoryRead)]
    public async Task<IActionResult> GetInventoryReport(
        [FromQuery] Guid? branchId = null,
        [FromQuery] Guid? bookId = null,
        [FromQuery] bool? lowStockOnly = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new GetInventoryReportQuery
                {
                    BranchId = branchId,
                    BookId = bookId,
                    LowStockOnly = lowStockOnly,
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("libros-mas-vendidos")]
    [Authorize(Policy = PermissionCodes.ReportsSalesRead)]
    public async Task<IActionResult> GetTopSellingBooks(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new GetTopSellingBooksReportQuery
                {
                    From = from,
                    To = to,
                    BranchId = branchId,
                    Limit = limit,
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("stock-bajo")]
    [Authorize(Policy = PermissionCodes.ReportsInventoryRead)]
    public async Task<IActionResult> GetLowStockReport(
        [FromQuery] Guid? branchId = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _sender.Send(
                new GetLowStockReportQuery
                {
                    BranchId = branchId,
                    Limit = limit,
                },
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
