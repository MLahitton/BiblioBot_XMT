using Application.Features.Catalog.Publishers.Common;
using MediatR;

namespace Application.Features.Catalog.Publishers.GetPublisherById;

public sealed class GetPublisherByIdQuery : IRequest<PublisherDto?>
{
    public Guid Id { get; init; }
}

