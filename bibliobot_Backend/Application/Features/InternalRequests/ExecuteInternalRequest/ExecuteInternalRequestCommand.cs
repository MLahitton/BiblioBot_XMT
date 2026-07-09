using Application.Features.InternalRequests.Common;
using MediatR;

namespace Application.Features.InternalRequests.ExecuteInternalRequest;

public sealed class ExecuteInternalRequestCommand : IRequest<InternalRequestDto>
{
    public Guid Id { get; init; }
    public string? Notes { get; init; }
}
