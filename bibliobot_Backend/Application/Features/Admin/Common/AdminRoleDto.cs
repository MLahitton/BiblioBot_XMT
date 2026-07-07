namespace Application.Features.Admin.Common;

public sealed class AdminRoleDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public int PermissionsCount { get; init; }
    public IReadOnlyCollection<AdminPermissionDto> Permissions { get; init; } = [];
}

