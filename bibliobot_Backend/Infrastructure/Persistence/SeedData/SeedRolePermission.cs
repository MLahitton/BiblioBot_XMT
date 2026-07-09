namespace Infrastructure.Persistence.SeedData;

public sealed class SeedRolePermission
{
    public SeedRolePermission(
        Guid id,
        Guid roleId,
        Guid permissionId,
        string roleCode,
        string permissionCode)
    {
        Id = id;
        RoleId = roleId;
        PermissionId = permissionId;
        RoleCode = roleCode;
        PermissionCode = permissionCode;
    }

    public Guid Id { get; }
    public Guid RoleId { get; }
    public Guid PermissionId { get; }
    public string RoleCode { get; }
    public string PermissionCode { get; }
}
