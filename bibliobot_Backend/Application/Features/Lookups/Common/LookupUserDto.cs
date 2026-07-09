using System;

namespace Application.Features.Lookups.Common;

public sealed class LookupUserDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; init; } = [];
    public bool IsActive { get; init; }
}

