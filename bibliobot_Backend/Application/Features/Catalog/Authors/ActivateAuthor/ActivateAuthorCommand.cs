using Application.Features.Catalog.Authors.Common;
using MediatR;

namespace Application.Features.Catalog.Authors.ActivateAuthor;

public sealed class ActivateAuthorCommand : IRequest<AuthorDto?>
{
    public Guid Id { get; init; }
}

