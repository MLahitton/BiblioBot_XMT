using Application.Features.Catalog.Authors.Common;
using MediatR;

namespace Application.Features.Catalog.Authors.GetAuthors;

public sealed class GetAuthorsQuery : IRequest<IReadOnlyCollection<AuthorDto>>
{
    public bool IncludeInactive { get; init; } = false;
}

