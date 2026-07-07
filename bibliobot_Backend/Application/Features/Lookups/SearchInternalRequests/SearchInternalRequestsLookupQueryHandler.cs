using Application.Common.DTOs;
using Application.Common.Interfaces;
using Application.Features.Lookups.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Lookups.SearchInternalRequests;

public sealed class SearchInternalRequestsLookupQueryHandler
    : IRequestHandler<SearchInternalRequestsLookupQuery, PagedResult<LookupInternalRequestDto>>
{
    private readonly IApplicationDbContext _context;

    public SearchInternalRequestsLookupQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LookupInternalRequestDto>> Handle(
        SearchInternalRequestsLookupQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        if (pageSize > 50)
        {
            pageSize = 50;
        }

        var query = _context.InternalRequests.AsNoTracking();

        var q = request.Q?.Trim();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var normalized = q!.ToUpperInvariant();
            query = query.Where(requestItem =>
                requestItem.Actor.FullName.ToUpper().Contains(normalized) ||
                requestItem.Actor.Email.ToUpper().Contains(normalized) ||
                requestItem.RequestType.Code.ToUpper().Contains(normalized) ||
                requestItem.RequestType.Name.ToUpper().Contains(normalized) ||
                requestItem.Status.Code.ToUpper().Contains(normalized) ||
                requestItem.Status.Name.ToUpper().Contains(normalized) ||
                (requestItem.SourceBranch != null && requestItem.SourceBranch.Name.ToUpper().Contains(normalized)) ||
                (requestItem.TargetBranch != null && requestItem.TargetBranch.Name.ToUpper().Contains(normalized)) ||
                (requestItem.Description != null && requestItem.Description.ToUpper().Contains(normalized)) ||
                (requestItem.Observations != null && requestItem.Observations.ToUpper().Contains(normalized)));
        }

        var requestTypeCode = request.RequestTypeCode?.Trim();
        if (!string.IsNullOrWhiteSpace(requestTypeCode))
        {
            var normalizedRequestType = requestTypeCode!.ToUpperInvariant();
            query = query.Where(requestItem => requestItem.RequestType.Code == normalizedRequestType);
        }

        var statusCode = request.StatusCode?.Trim();
        if (!string.IsNullOrWhiteSpace(statusCode))
        {
            var normalizedStatus = statusCode!.ToUpperInvariant();
            query = query.Where(requestItem => requestItem.Status.Code == normalizedStatus);
        }

        var requestedByEmail = request.RequestedByEmail?.Trim();
        if (!string.IsNullOrWhiteSpace(requestedByEmail))
        {
            var normalizedEmail = requestedByEmail!.ToUpperInvariant();
            query = query.Where(requestItem => requestItem.Actor.Email.ToUpper().Contains(normalizedEmail));
        }

        var branchName = request.BranchName?.Trim();
        if (!string.IsNullOrWhiteSpace(branchName))
        {
            var normalizedBranch = branchName!.ToUpperInvariant();
            query = query.Where(requestItem =>
                (requestItem.SourceBranch != null && requestItem.SourceBranch.Name.ToUpper().Contains(normalizedBranch)) ||
                (requestItem.TargetBranch != null && requestItem.TargetBranch.Name.ToUpper().Contains(normalizedBranch)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(requestItem => requestItem.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(requestItem => new LookupInternalRequestDto
            {
                Id = requestItem.Id,
                RequestTypeCode = requestItem.RequestType.Code,
                StatusCode = requestItem.Status.Code,
                RequestedByName = requestItem.Actor.FullName,
                RequestedByEmail = requestItem.Actor.Email,
                SourceBranchName = requestItem.SourceBranch != null ? requestItem.SourceBranch.Name : null,
                DestinationBranchName = requestItem.TargetBranch != null ? requestItem.TargetBranch.Name : null,
                CreatedAt = requestItem.CreatedAt,
                Label = BuildLabel(
                    requestItem.RequestType.Code,
                    requestItem.Actor.FullName,
                    requestItem.Status.Code),
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupInternalRequestDto>(items, pageNumber, pageSize, totalCount);
    }

    private static string BuildLabel(string requestTypeCode, string requestedByName, string statusCode)
    {
        return $"{requestTypeCode} - {requestedByName} - {statusCode}";
    }
}

