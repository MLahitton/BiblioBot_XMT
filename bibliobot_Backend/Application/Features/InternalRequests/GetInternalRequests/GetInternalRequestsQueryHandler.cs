using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.InternalRequests.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InternalRequests.GetInternalRequests;

public sealed class GetInternalRequestsQueryHandler : IRequestHandler<GetInternalRequestsQuery, PagedResult<InternalRequestListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetInternalRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<InternalRequestListItemDto>> Handle(
        GetInternalRequestsQuery request,
        CancellationToken cancellationToken)
    {
        if (!request.CanReadAll && !request.CanReadOwn)
        {
            throw new UnauthorizedAccessException("No tienes permisos para consultar solicitudes.");
        }

        if (request.RequestedByUserId.HasValue && !request.CanReadAll && request.RequestedByUserId.Value != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("No tienes permisos para consultar solicitudes ajenas.");
        }

        if (request.From.HasValue && request.To.HasValue && request.From.Value > request.To.Value)
        {
            throw new ArgumentException("Rango de fechas invalido.");
        }

        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 100)
        {
            pageSize = 100;
        }

        var query = _context.InternalRequests.AsNoTracking()
            .Include(internalRequest => internalRequest.RequestType)
            .Include(internalRequest => internalRequest.Status)
            .Include(internalRequest => internalRequest.Actor)
            .Include(internalRequest => internalRequest.SourceBranch)
            .Include(internalRequest => internalRequest.TargetBranch)
            .Include(internalRequest => internalRequest.Items)
            .AsQueryable();

        if (!request.CanReadAll)
        {
            query = query.Where(internalRequest => internalRequest.ActorId == request.CurrentUserId);
        }
        else if (request.RequestedByUserId.HasValue)
        {
            query = query.Where(internalRequest => internalRequest.ActorId == request.RequestedByUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.RequestTypeCode))
        {
            var requestTypeCode = request.RequestTypeCode!.Trim().ToUpperInvariant();
            query = query.Where(internalRequest => internalRequest.RequestType.Code.ToUpper() == requestTypeCode);
        }

        if (!string.IsNullOrWhiteSpace(request.StatusCode))
        {
            var statusCode = request.StatusCode!.Trim().ToUpperInvariant();
            query = query.Where(internalRequest => internalRequest.Status.Code.ToUpper() == statusCode);
        }

        if (request.BranchId.HasValue)
        {
            var branchId = request.BranchId.Value;
            query = query.Where(internalRequest =>
                internalRequest.SourceBranchId == branchId || internalRequest.TargetBranchId == branchId);
        }

        if (request.From.HasValue)
        {
            query = query.Where(internalRequest => internalRequest.CreatedAt >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(internalRequest => internalRequest.CreatedAt <= request.To.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(internalRequest => internalRequest.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(internalRequest => new InternalRequestListItemDto
            {
                Id = internalRequest.Id,
                RequestTypeCode = internalRequest.RequestType.Code,
                RequestTypeName = internalRequest.RequestType.Name,
                StatusCode = internalRequest.Status.Code,
                StatusName = internalRequest.Status.Name,
                RequestedByUserId = internalRequest.ActorId,
                RequestedByUserName = internalRequest.Actor.FullName,
                SourceBranchId = internalRequest.SourceBranchId,
                SourceBranchName = internalRequest.SourceBranch == null ? null : internalRequest.SourceBranch.Name,
                DestinationBranchId = internalRequest.TargetBranchId,
                DestinationBranchName = internalRequest.TargetBranch == null ? null : internalRequest.TargetBranch.Name,
                CreatedAt = internalRequest.CreatedAt,
                UpdatedAt = internalRequest.ExecutedAt ?? internalRequest.ReviewedAt ?? internalRequest.CreatedAt,
                TotalItems = internalRequest.Items.Count,
                TotalQuantity = internalRequest.Items.Sum(item => item.Quantity),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InternalRequestListItemDto>(items, pageNumber, pageSize, totalCount);
    }
}
