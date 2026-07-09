using Application.Features.Catalog.Authors.Common;
using MediatR;

namespace Application.Features.Catalog.Authors.UpdateAuthor;

public sealed class UpdateAuthorCommand : IRequest<AuthorDto?>
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
}

