using Application.Features.InternalRequests.Common;
using MediatR;

namespace Application.Features.InternalRequests.GetInternalRequestById;

public sealed class GetInternalRequestByIdQuery : IRequest<InternalRequestDto?>
{
    public Guid Id { get; init; }
    public bool CanReadAll { get; init; }
    public bool CanReadOwn { get; init; }
    public Guid CurrentUserId { get; init; }
}
