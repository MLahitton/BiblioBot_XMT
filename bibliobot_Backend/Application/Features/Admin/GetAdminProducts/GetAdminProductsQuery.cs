using Application.Common.DTOs;
using Application.Features.Admin.Common;
using MediatR;

namespace Application.Features.Admin.GetAdminProducts;

public sealed class GetAdminProductsQuery : IRequest<PagedResult<AdminProductDto>>
{
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public string? SortBy { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
