namespace Application.Features.Catalog.Authors.Common;

public sealed class AuthorDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

