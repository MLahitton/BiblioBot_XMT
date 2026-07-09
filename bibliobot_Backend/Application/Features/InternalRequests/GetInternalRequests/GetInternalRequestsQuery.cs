using Application.Common.DTOs;
using Application.Features.InternalRequests.Common;
using MediatR;

namespace Application.Features.InternalRequests.GetInternalRequests;

public sealed class GetInternalRequestsQuery : IRequest<PagedResult<InternalRequestListItemDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? RequestTypeCode { get; init; }
    public string? StatusCode { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? RequestedByUserId { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public bool CanReadAll { get; init; }
    public bool CanReadOwn { get; init; }
    public Guid CurrentUserId { get; init; }
}
