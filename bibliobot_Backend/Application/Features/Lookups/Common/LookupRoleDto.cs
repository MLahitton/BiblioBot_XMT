using System;

namespace Application.Features.Lookups.Common;

public sealed class LookupRoleDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int PermissionsCount { get; init; }
}

