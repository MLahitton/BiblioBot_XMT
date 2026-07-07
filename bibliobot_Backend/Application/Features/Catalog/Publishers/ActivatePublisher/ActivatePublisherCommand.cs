using Application.Features.Catalog.Publishers.Common;
using MediatR;

namespace Application.Features.Catalog.Publishers.ActivatePublisher;

public sealed class ActivatePublisherCommand : IRequest<PublisherDto?>
{
    public Guid Id { get; init; }
}

