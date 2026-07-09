using System;

namespace Application.Features.Lookups.Common;

public sealed class LookupBookDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Isbn { get; init; }
    public decimal Price { get; init; }
    public string? PublisherName { get; init; }
    public IReadOnlyCollection<string> Authors { get; init; } = [];
    public IReadOnlyCollection<string> Categories { get; init; } = [];
    public int TotalStock { get; init; }
    public bool IsActive { get; init; }
}

