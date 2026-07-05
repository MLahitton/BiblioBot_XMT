using Application.Common.Interfaces;
using Application.Features.InternalRequests.Common;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.InternalRequests.CreateTransferRequest;

public sealed class CreateTransferRequestCommandHandler : IRequestHandler<CreateTransferRequestCommand, InternalRequestDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateTransferRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<InternalRequestDto> Handle(
        CreateTransferRequestCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        if (request.SourceBranchId == request.DestinationBranchId)
        {
            throw new ArgumentException("Las sedes de origen y destino deben ser diferentes.");
        }

        var actorId = _currentUserService.UserId.Value;
        var actor = await _context.Users.FirstOrDefaultAsync(
            user => user.Id == actorId,
            cancellationToken);

        if (actor is null)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var sourceBranchExists = await _context.Branches.AnyAsync(
            branch => branch.Id == request.SourceBranchId && branch.IsActive,
            cancellationToken);

        if (!sourceBranchExists)
        {
            throw new KeyNotFoundException("Sede de origen no encontrada.");
        }

        var destinationBranchExists = await _context.Branches.AnyAsync(
            branch => branch.Id == request.DestinationBranchId && branch.IsActive,
            cancellationToken);

        if (!destinationBranchExists)
        {
            throw new KeyNotFoundException("Sede de destino no encontrada.");
        }

        var consolidatedItems = ConsolidateItems(request.Items, out var hasInvalidItem);
        if (hasInvalidItem)
        {
            throw new ArgumentException("Cada item debe tener cantidad mayor a 0.");
        }

        if (consolidatedItems.Count == 0)
        {
            throw new ArgumentException("La solicitud debe incluir al menos un item.");
        }

        var bookIds = consolidatedItems.Select(item => item.Key).ToList();
        var books = await _context.Books
            .Where(book => bookIds.Contains(book.Id) && book.IsActive && !book.IsDeleted)
            .ToListAsync(cancellationToken);

        if (books.Count != consolidatedItems.Count)
        {
            throw new KeyNotFoundException("Libro no encontrado.");
        }

        var requestType = await _context.RequestTypes.FirstOrDefaultAsync(
            requestType => requestType.Code == RequestTypeCodes.Transfer,
            cancellationToken);

        if (requestType is null)
        {
            throw new KeyNotFoundException("Tipo de solicitud no encontrado.");
        }

        var requestStatus = await _context.RequestStatuses.FirstOrDefaultAsync(
            status => status.Code == RequestStatusCodes.Created,
            cancellationToken);

        if (requestStatus is null)
        {
            throw new KeyNotFoundException("Estado de solicitud no encontrado.");
        }

        var now = DateTimeOffset.UtcNow;
        var description = request.Notes?.Trim();

        var requestEntity = new InternalRequest
        {
            RequestTypeId = requestType.Id,
            StatusId = requestStatus.Id,
            ActorId = actorId,
            SourceBranchId = request.SourceBranchId,
            TargetBranchId = request.DestinationBranchId,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            CreatedAt = now,
            Items = consolidatedItems
                .Select(item => new InternalRequestItem
                {
                    BookId = item.Key,
                    RequestedTitle = books.First(book => book.Id == item.Key).Title,
                    Quantity = item.Value,
                })
                .ToList(),
        };

        _context.InternalRequests.Add(requestEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetInternalRequestDtoAsync(requestEntity.Id, cancellationToken);
    }

    public async Task<InternalRequestDto> GetInternalRequestDtoAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var requestEntity = await _context.InternalRequests.AsNoTracking()
            .Include(internalRequest => internalRequest.RequestType)
            .Include(internalRequest => internalRequest.Status)
            .Include(internalRequest => internalRequest.Actor)
            .Include(internalRequest => internalRequest.SourceBranch)
            .Include(internalRequest => internalRequest.TargetBranch)
            .Include(internalRequest => internalRequest.Items)
                .ThenInclude(item => item.Book)
            .FirstOrDefaultAsync(internalRequest => internalRequest.Id == requestId, cancellationToken);

        if (requestEntity is null)
        {
            throw new KeyNotFoundException("Solicitud no encontrada.");
        }

        return new InternalRequestDto
        {
            Id = requestEntity.Id,
            RequestTypeCode = requestEntity.RequestType?.Code ?? string.Empty,
            RequestTypeName = requestEntity.RequestType?.Name ?? string.Empty,
            StatusCode = requestEntity.Status?.Code ?? string.Empty,
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

    private static IReadOnlyDictionary<Guid, int> ConsolidateItems(
        IReadOnlyCollection<CreateInternalRequestItemCommand> items,
        out bool hasInvalidItem)
    {
        var dict = new Dictionary<Guid, int>();

        hasInvalidItem = false;

        foreach (var item in items)
        {
            if (item.BookId == Guid.Empty || item.Quantity <= 0)
            {
                hasInvalidItem = true;
                continue;
            }

            if (!dict.TryGetValue(item.BookId, out var existingQuantity))
            {
                dict[item.BookId] = item.Quantity;
            }
            else
            {
                dict[item.BookId] = existingQuantity + item.Quantity;
            }
        }

        return dict;
    }
}
