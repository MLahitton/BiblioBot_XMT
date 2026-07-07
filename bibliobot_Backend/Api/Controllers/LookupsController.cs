using System.Security.Claims;
using Application.Common.DTOs;
using Application.Features.Lookups.Common;
using Application.Features.Lookups.SearchAuthors;
using Application.Features.Lookups.SearchBranches;
using Application.Features.Lookups.SearchBooks;
using Application.Features.Lookups.SearchCategories;
using Application.Features.Lookups.SearchInternalRequests;
using Application.Features.Lookups.SearchInvoices;
using Application.Features.Lookups.SearchPublishers;
using Application.Features.Lookups.SearchRoles;
using Application.Features.Lookups.SearchSales;
using Application.Features.Lookups.SearchUsers;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/busquedas")]
public sealed class LookupsController : ControllerBase
{
    private readonly ISender _sender;

    public LookupsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("libros")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchBooks(
        [FromQuery] string? q,
        [FromQuery] string? isbn,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchBooksLookupQuery
        {
            Q = q,
            Isbn = isbn,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("autores")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchAuthors(
        [FromQuery] string? q,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchAuthorsLookupQuery
        {
            Q = q,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("categorias")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchCategories(
        [FromQuery] string? q,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchCategoriesLookupQuery
        {
            Q = q,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("editoriales")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchPublishers(
        [FromQuery] string? q,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchPublishersLookupQuery
        {
            Q = q,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("sedes")]
    [Authorize(Policy = PermissionCodes.InventoryRead)]
    public async Task<IActionResult> SearchBranches(
        [FromQuery] string? q,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchBranchesLookupQuery
        {
            Q = q,
            IsActive = isActive,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("usuarios")]
    [Authorize(Policy = PermissionCodes.AdminUsersRead)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string? q,
        [FromQuery] string? email,
        [FromQuery] string? roleCode,
        [FromQuery] bool? isActive,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchUsersLookupQuery
        {
            Q = q,
            Email = email,
            RoleCode = roleCode,
            IsActive = isActive,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("roles")]
    [Authorize(Policy = PermissionCodes.AdminRolesRead)]
    public async Task<IActionResult> SearchRoles(
        [FromQuery] string? q,
        [FromQuery] string? code,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchRolesLookupQuery
        {
            Q = q,
            Code = code,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("ventas")]
    [Authorize(Policy = PermissionCodes.SalesReadAll)]
    public async Task<IActionResult> SearchSales(
        [FromQuery] string? q,
        [FromQuery] string? customerEmail,
        [FromQuery] string? statusCode,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchSalesLookupQuery
        {
            Q = q,
            CustomerEmail = customerEmail,
            StatusCode = statusCode,
            From = from,
            To = to,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("facturas")]
    [Authorize(Policy = PermissionCodes.InvoicesReadAll)]
    public async Task<IActionResult> SearchInvoices(
        [FromQuery] string? q,
        [FromQuery] string? invoiceNumber,
        [FromQuery] string? customerEmail,
        [FromQuery] Guid? saleId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchInvoicesLookupQuery
        {
            Q = q,
            InvoiceNumber = invoiceNumber,
            CustomerEmail = customerEmail,
            SaleId = saleId,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    [HttpGet("solicitudes")]
    [Authorize(Policy = PermissionCodes.RequestsRead)]
    public async Task<IActionResult> SearchInternalRequests(
        [FromQuery] string? q,
        [FromQuery] string? requestTypeCode,
        [FromQuery] string? statusCode,
        [FromQuery] string? requestedByEmail,
        [FromQuery] string? branchName,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new SearchInternalRequestsLookupQuery
        {
            Q = q,
            RequestTypeCode = requestTypeCode,
            StatusCode = statusCode,
            RequestedByEmail = requestedByEmail,
            BranchName = branchName,
            PageNumber = pageNumber,
            PageSize = pageSize,
        }, cancellationToken);

        return Ok(result);
    }

    private static bool HasPermission(ClaimsPrincipal principal, string permission)
    {
        return principal.Claims.Any(claim => claim.Type == "permission" && claim.Value == permission);
    }
}

