namespace Application.Features.Admin.Common;

public sealed class AdminUserDetailDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public IReadOnlyCollection<AdminRoleDto> Roles { get; init; } = [];
    public IReadOnlyCollection<AdminPermissionDto> EffectivePermissions { get; init; } = [];
}

