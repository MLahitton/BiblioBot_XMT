using Application.Features.Catalog.Authors.Common;
using MediatR;

namespace Application.Features.Catalog.Authors.DisableAuthor;

public sealed class DisableAuthorCommand : IRequest<AuthorDto?>
{
    public Guid Id { get; init; }
}

