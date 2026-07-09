using Application.Common.Interfaces;
using Application.Features.InternalRequests.Common;
using Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InternalRequests.GetInternalRequestById;

public sealed class GetInternalRequestByIdQueryHandler : IRequestHandler<GetInternalRequestByIdQuery, InternalRequestDto?>
{
    private readonly IApplicationDbContext _context;

    public GetInternalRequestByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InternalRequestDto?> Handle(
        GetInternalRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!request.CanReadAll && !request.CanReadOwn)
        {
            throw new UnauthorizedAccessException("No tienes permisos para consultar solicitudes.");
        }

        var requestEntity = await _context.InternalRequests.AsNoTracking()
            .Include(internalRequest => internalRequest.RequestType)
            .Include(internalRequest => internalRequest.Status)
            .Include(internalRequest => internalRequest.Actor)
            .Include(internalRequest => internalRequest.SourceBranch)
            .Include(internalRequest => internalRequest.TargetBranch)
            .Include(internalRequest => internalRequest.Items)
                .ThenInclude(item => item.Book)
            .FirstOrDefaultAsync(internalRequest => internalRequest.Id == request.Id, cancellationToken);

        if (requestEntity is null)
        {
            return null;
        }

        var statusCode = requestEntity.Status?.Code ?? string.Empty;
        if (!request.CanReadAll && requestEntity.ActorId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("No tienes permisos para consultar esta solicitud.");
        }

        return new InternalRequestDto
        {
            Id = requestEntity.Id,
            RequestTypeCode = requestEntity.RequestType?.Code ?? string.Empty,
            RequestTypeName = requestEntity.RequestType?.Name ?? string.Empty,
            StatusCode = statusCode,
            StatusName = requestEntity.Status?.Name ?? string.Empty,
            RequestedByUserId = requestEntity.ActorId,
            RequestedByUserName = requestEntity.Actor?.FullName ?? string.Empty,
            SourceBranchId = requestEntity.SourceBranchId,
            SourceBranchName = requestEntity.SourceBranch?.Name,
            DestinationBranchId = requestEntity.TargetBranchId,
            DestinationBranchName = requestEntity.TargetBranch?.Name,
            Notes = requestEntity.Description,
            CreatedAt = requestEntity.CreatedAt,
            UpdatedAt = requestEntity.ExecutedAt ?? requestEntity.ReviewedAt ?? requestEntity.CreatedAt,
            ApprovedAt = statusCode == RequestStatusCodes.Approved ? requestEntity.ReviewedAt : null,
            RejectedAt = statusCode == RequestStatusCodes.Rejected ? requestEntity.ReviewedAt : null,
            ExecutedAt = requestEntity.ExecutedAt,
            Items = requestEntity.Items
                .Select(item => new InternalRequestItemDto
                {
                    Id = item.Id,
                    BookId = item.BookId ?? Guid.Empty,
                    BookTitle = item.RequestedTitle ?? item.Book?.Title ?? string.Empty,
                    Isbn = item.Book?.Isbn,
                    Quantity = item.Quantity,
                })
                .ToList(),
        };
    }
}
