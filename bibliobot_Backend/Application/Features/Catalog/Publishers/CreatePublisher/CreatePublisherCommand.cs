using Application.Features.Catalog.Publishers.Common;
using MediatR;

namespace Application.Features.Catalog.Publishers.CreatePublisher;

public sealed class CreatePublisherCommand : IRequest<PublisherDto>
{
    public string Name { get; init; } = string.Empty;
}

