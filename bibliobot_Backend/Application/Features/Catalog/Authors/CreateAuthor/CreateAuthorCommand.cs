using Application.Features.Catalog.Authors.Common;
using MediatR;

namespace Application.Features.Catalog.Authors.CreateAuthor;

public sealed class CreateAuthorCommand : IRequest<AuthorDto>
{
    public string FullName { get; init; } = string.Empty;
}

