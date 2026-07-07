using Application.Features.InternalRequests.Common;
using MediatR;

namespace Application.Features.InternalRequests.ApproveInternalRequest;

public sealed class ApproveInternalRequestCommand : IRequest<InternalRequestDto>
{
    public Guid Id { get; init; }
    public string? Notes { get; init; }
}
