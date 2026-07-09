using Application.Features.Catalog.Publishers.Common;
using MediatR;

namespace Application.Features.Catalog.Publishers.DisablePublisher;

public sealed class DisablePublisherCommand : IRequest<PublisherDto?>
{
    public Guid Id { get; init; }
}

