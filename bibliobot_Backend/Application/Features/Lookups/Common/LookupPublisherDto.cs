using System;

namespace Application.Features.Lookups.Common;

public sealed class LookupPublisherDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

