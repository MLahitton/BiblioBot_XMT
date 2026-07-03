using System.Collections.Generic;
using System.Linq;
using Domain.Constants;

namespace Infrastructure.Persistence.SeedData;

public static class RolePermissionSeedData
{
    public static IReadOnlyCollection<SeedRolePermission> RolePermissions { get; } = new[]
    {
        Create(1, RoleCodes.Client, PermissionCodes.AuthMe),
        Create(2, RoleCodes.Client, PermissionCodes.AuthLogout),
        Create(3, RoleCodes.Client, PermissionCodes.AuthChangePassword),
        Create(4, RoleCodes.Client, PermissionCodes.BooksRead),
        Create(5, RoleCodes.Client, PermissionCodes.BooksSearch),
        Create(6, RoleCodes.Client, PermissionCodes.CartManage),
        Create(7, RoleCodes.Client, PermissionCodes.CartRead),
        Create(8, RoleCodes.Client, PermissionCodes.SalesCreate),
        Create(9, RoleCodes.Client, PermissionCodes.SalesConfirm),
        Create(10, RoleCodes.Client, PermissionCodes.SalesReadOwn),
        Create(11, RoleCodes.Client, PermissionCodes.InvoicesReadOwn),
        Create(12, RoleCodes.Client, PermissionCodes.ChatMessage),

        Create(12 + 1, RoleCodes.Worker, PermissionCodes.AuthMe),
        Create(12 + 2, RoleCodes.Worker, PermissionCodes.AuthLogout),
        Create(12 + 3, RoleCodes.Worker, PermissionCodes.AuthChangePassword),
        Create(12 + 4, RoleCodes.Worker, PermissionCodes.BooksRead),
        Create(12 + 5, RoleCodes.Worker, PermissionCodes.BooksSearch),
        Create(12 + 6, RoleCodes.Worker, PermissionCodes.BooksCreate),
        Create(12 + 7, RoleCodes.Worker, PermissionCodes.BooksUpdate),
        Create(12 + 8, RoleCodes.Worker, PermissionCodes.InventoryRead),
        Create(12 + 9, RoleCodes.Worker, PermissionCodes.InventoryEntry),
        Create(12 + 10, RoleCodes.Worker, PermissionCodes.InventoryExit),
        Create(12 + 11, RoleCodes.Worker, PermissionCodes.InventoryAdjust),
        Create(12 + 12, RoleCodes.Worker, PermissionCodes.RequestsPurchaseCreate),
        Create(12 + 13, RoleCodes.Worker, PermissionCodes.RequestsTransferCreate),
        Create(12 + 14, RoleCodes.Worker, PermissionCodes.RequestsRead),
        Create(12 + 15, RoleCodes.Worker, PermissionCodes.SalesReadAll),
        Create(12 + 16, RoleCodes.Worker, PermissionCodes.InvoicesReadAll),
        Create(12 + 17, RoleCodes.Worker, PermissionCodes.ChatLogsRead),
        Create(12 + 18, RoleCodes.Worker, PermissionCodes.ReportsInventoryRead),

        Create(12 + 18 + 1, RoleCodes.Admin, PermissionCodes.AuthMe),
        Create(12 + 18 + 2, RoleCodes.Admin, PermissionCodes.AuthLogout),
        Create(12 + 18 + 3, RoleCodes.Admin, PermissionCodes.AuthChangePassword),
        Create(12 + 18 + 4, RoleCodes.Admin, PermissionCodes.BooksRead),
        Create(12 + 18 + 5, RoleCodes.Admin, PermissionCodes.BooksSearch),
        Create(12 + 18 + 6, RoleCodes.Admin, PermissionCodes.BooksCreate),
        Create(12 + 18 + 7, RoleCodes.Admin, PermissionCodes.BooksUpdate),
        Create(12 + 18 + 8, RoleCodes.Admin, PermissionCodes.BooksDisable),
        Create(12 + 18 + 9, RoleCodes.Admin, PermissionCodes.BooksActivate),
        Create(12 + 18 + 10, RoleCodes.Admin, PermissionCodes.CartRead),
        Create(12 + 18 + 11, RoleCodes.Admin, PermissionCodes.SalesCreate),
        Create(12 + 18 + 12, RoleCodes.Admin, PermissionCodes.SalesConfirm),
        Create(12 + 18 + 13, RoleCodes.Admin, PermissionCodes.SalesReadOwn),
        Create(12 + 18 + 14, RoleCodes.Admin, PermissionCodes.SalesReadAll),
        Create(12 + 18 + 15, RoleCodes.Admin, PermissionCodes.SalesCancel),
        Create(12 + 18 + 16, RoleCodes.Admin, PermissionCodes.InvoicesReadOwn),
        Create(12 + 18 + 17, RoleCodes.Admin, PermissionCodes.InvoicesReadAll),
        Create(12 + 18 + 18, RoleCodes.Admin, PermissionCodes.InventoryRead),
        Create(12 + 18 + 19, RoleCodes.Admin, PermissionCodes.InventoryEntry),
        Create(12 + 18 + 20, RoleCodes.Admin, PermissionCodes.InventoryExit),
        Create(12 + 18 + 21, RoleCodes.Admin, PermissionCodes.InventoryAdjust),
        Create(12 + 18 + 22, RoleCodes.Admin, PermissionCodes.RequestsPurchaseCreate),
        Create(12 + 18 + 23, RoleCodes.Admin, PermissionCodes.RequestsTransferCreate),
        Create(12 + 18 + 24, RoleCodes.Admin, PermissionCodes.RequestsRead),
        Create(12 + 18 + 25, RoleCodes.Admin, PermissionCodes.RequestsReview),
        Create(12 + 18 + 26, RoleCodes.Admin, PermissionCodes.RequestsApprove),
        Create(12 + 18 + 27, RoleCodes.Admin, PermissionCodes.RequestsReject),
        Create(12 + 18 + 28, RoleCodes.Admin, PermissionCodes.RequestsExecute),
        Create(12 + 18 + 29, RoleCodes.Admin, PermissionCodes.AdminUsersRead),
        Create(12 + 18 + 30, RoleCodes.Admin, PermissionCodes.AdminRolesRead),
        Create(12 + 18 + 31, RoleCodes.Admin, PermissionCodes.AdminPermissionsRead),
        Create(12 + 18 + 32, RoleCodes.Admin, PermissionCodes.AdminPermissionsManage),
        Create(12 + 18 + 33, RoleCodes.Admin, PermissionCodes.ChatMessage),
        Create(12 + 18 + 34, RoleCodes.Admin, PermissionCodes.ChatLogsRead),
        Create(12 + 18 + 35, RoleCodes.Admin, PermissionCodes.ReportsSalesRead),
        Create(12 + 18 + 36, RoleCodes.Admin, PermissionCodes.ReportsInventoryRead)
    };

    private static Guid GetRoleId(string roleCode)
    {
        return AuthSeedData.Roles.Single(role => role.Code == roleCode).Id;
    }

    private static Guid GetPermissionId(string permissionCode)
    {
        return AuthSeedData.Permissions.Single(permission => permission.Code == permissionCode).Id;
    }

    private static Guid SeedId(int number)
    {
        return new Guid($"30000000-0000-0000-0000-{number:000000000000}");
    }

    private static SeedRolePermission Create(int number, string roleCode, string permissionCode)
    {
        return new SeedRolePermission(
            SeedId(number),
            GetRoleId(roleCode),
            GetPermissionId(permissionCode),
            roleCode,
            permissionCode);
    }
}
