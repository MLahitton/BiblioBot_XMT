using Application.Features.Catalog.Publishers.Common;
using MediatR;

namespace Application.Features.Catalog.Publishers.GetPublishers;

public sealed class GetPublishersQuery : IRequest<IReadOnlyCollection<PublisherDto>>
{
    public bool IncludeInactive { get; init; } = false;
}

