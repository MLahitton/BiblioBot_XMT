using Application.Features.Catalog.Publishers.Common;
using MediatR;

namespace Application.Features.Catalog.Publishers.UpdatePublisher;

public sealed class UpdatePublisherCommand : IRequest<PublisherDto?>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

