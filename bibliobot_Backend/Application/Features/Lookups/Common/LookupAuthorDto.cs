using System;

namespace Application.Features.Lookups.Common;

public sealed class LookupAuthorDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

