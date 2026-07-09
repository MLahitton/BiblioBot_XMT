namespace Application.Features.Catalog.Publishers.Common;

public sealed class PublisherDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

