using System;

namespace Application.Features.Lookups.Common;

public sealed class LookupBranchDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? City { get; init; }
    public bool IsActive { get; init; }
}

