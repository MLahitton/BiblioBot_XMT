using Application.Features.Catalog.Authors.Common;
using MediatR;

namespace Application.Features.Catalog.Authors.GetAuthorById;

public sealed class GetAuthorByIdQuery : IRequest<AuthorDto?>
{
    public Guid Id { get; init; }
}

