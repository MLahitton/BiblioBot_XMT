using Application.Common.Interfaces;
using Application.Features.InternalRequests.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InternalRequests.ApproveInternalRequest;

public sealed class ApproveInternalRequestCommandHandler : IRequestHandler<ApproveInternalRequestCommand, InternalRequestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ApproveInternalRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InternalRequestDto> Handle(
        ApproveInternalRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var requestEntity = await _context.InternalRequests
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
            throw new KeyNotFoundException("Solicitud no encontrada.");
        }

        var statusCode = requestEntity.Status?.Code ?? string.Empty;

        if (statusCode == RequestStatusCodes.Approved)
        {
            return MapToDto(requestEntity);
        }

        if (statusCode is not (RequestStatusCodes.Created or RequestStatusCodes.InReview))
        {
            throw new InvalidOperationException("La solicitud no puede aprobarse en su estado actual.");
        }

        var approvedStatus = await _context.RequestStatuses.FirstOrDefaultAsync(
            status => status.Code == RequestStatusCodes.Approved,
            cancellationToken);

        if (approvedStatus is null)
        {
            throw new KeyNotFoundException("Estado de solicitud no encontrado.");
        }

        var now = DateTimeOffset.UtcNow;
        var notes = request.Notes?.Trim();
        var existingNotes = requestEntity.Description;

        requestEntity.StatusId = approvedStatus.Id;
        requestEntity.ReviewedAt = now;

        if (!string.IsNullOrWhiteSpace(notes))
        {
            requestEntity.Description = string.IsNullOrWhiteSpace(existingNotes)
                ? notes
                : $"{existingNotes} | {notes}";
        }

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(requestEntity);
    }

    private static InternalRequestDto MapToDto(InternalRequest requestEntity)
    {
        var statusCode = requestEntity.Status?.Code ?? string.Empty;

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
