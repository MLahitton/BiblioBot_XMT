using Application.Features.InternalRequests.Common;
using MediatR;

namespace Application.Features.InternalRequests.RejectInternalRequest;

public sealed class RejectInternalRequestCommand : IRequest<InternalRequestDto>
{
    public Guid Id { get; init; }
    public string Reason { get; init; } = string.Empty;
}
