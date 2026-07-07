using System.Collections.Generic;

using Domain.Constants;

namespace Infrastructure.Persistence.SeedData;

public static class AuthSeedData
{
    public static IReadOnlyCollection<SeedRole> Roles { get; } = new[]
    {
        new SeedRole(
            RoleSeedId(1),
            RoleCodes.Client,
            "Cliente",
            "Cliente final del sistema"),
        new SeedRole(
            RoleSeedId(2),
            RoleCodes.Worker,
            "Trabajador",
            "Empleado interno del sistema"),
        new SeedRole(
            RoleSeedId(3),
            RoleCodes.Admin,
            "Administrador",
            "Administrador del sistema")
    };

    public static IReadOnlyCollection<SeedPermission> Permissions { get; } = new[]
    {
        new SeedPermission(PermissionSeedId(1), PermissionCodes.AuthMe, "Ver mi usuario", null),
        new SeedPermission(PermissionSeedId(2), PermissionCodes.AuthLogout, "Cerrar sesion", null),
        new SeedPermission(PermissionSeedId(3), PermissionCodes.AuthChangePassword, "Cambiar contrasena", null),
        new SeedPermission(PermissionSeedId(4), PermissionCodes.BooksRead, "Ver libros", null),
        new SeedPermission(PermissionSeedId(5), PermissionCodes.BooksSearch, "Buscar libros", null),
        new SeedPermission(PermissionSeedId(6), PermissionCodes.BooksCreate, "Crear libros", null),
        new SeedPermission(PermissionSeedId(7), PermissionCodes.BooksUpdate, "Actualizar libros", null),
        new SeedPermission(PermissionSeedId(8), PermissionCodes.BooksDisable, "Desactivar libros", null),
        new SeedPermission(PermissionSeedId(9), PermissionCodes.BooksActivate, "Activar libros", null),
        new SeedPermission(PermissionSeedId(10), PermissionCodes.CartManage, "Administrar carrito", null),
        new SeedPermission(PermissionSeedId(11), PermissionCodes.CartRead, "Leer carrito", null),
        new SeedPermission(PermissionSeedId(12), PermissionCodes.SalesCreate, "Crear venta", null),
        new SeedPermission(PermissionSeedId(13), PermissionCodes.SalesConfirm, "Confirmar venta", null),
        new SeedPermission(PermissionSeedId(14), PermissionCodes.SalesReadOwn, "Ver ventas propias", null),
        new SeedPermission(PermissionSeedId(15), PermissionCodes.SalesReadAll, "Ver todas las ventas", null),
        new SeedPermission(PermissionSeedId(16), PermissionCodes.SalesCancel, "Cancelar venta", null),
        new SeedPermission(PermissionSeedId(17), PermissionCodes.InvoicesReadOwn, "Ver facturas propias", null),
        new SeedPermission(PermissionSeedId(18), PermissionCodes.InvoicesReadAll, "Ver todas las facturas", null),
        new SeedPermission(PermissionSeedId(19), PermissionCodes.InventoryRead, "Ver inventario", null),
        new SeedPermission(PermissionSeedId(20), PermissionCodes.InventoryEntry, "Registrar entrada inventario", null),
        new SeedPermission(PermissionSeedId(21), PermissionCodes.InventoryExit, "Registrar salida inventario", null),
        new SeedPermission(PermissionSeedId(22), PermissionCodes.InventoryAdjust, "Ajustar inventario", null),
        new SeedPermission(PermissionSeedId(23), PermissionCodes.RequestsPurchaseCreate, "Crear solicitud de compra", null),
        new SeedPermission(PermissionSeedId(24), PermissionCodes.RequestsTransferCreate, "Crear solicitud de traslado", null),
        new SeedPermission(PermissionSeedId(25), PermissionCodes.RequestsRead, "Ver solicitudes", null),
        new SeedPermission(PermissionSeedId(26), PermissionCodes.RequestsReview, "Revisar solicitudes", null),
        new SeedPermission(PermissionSeedId(27), PermissionCodes.RequestsApprove, "Aprobar solicitudes", null),
        new SeedPermission(PermissionSeedId(28), PermissionCodes.RequestsReject, "Rechazar solicitudes", null),
        new SeedPermission(PermissionSeedId(29), PermissionCodes.RequestsExecute, "Ejecutar solicitudes", null),
        new SeedPermission(PermissionSeedId(30), PermissionCodes.AdminUsersRead, "Ver usuarios", null),
        new SeedPermission(PermissionSeedId(31), PermissionCodes.AdminRolesRead, "Ver roles", null),
        new SeedPermission(PermissionSeedId(32), PermissionCodes.AdminPermissionsRead, "Ver permisos", null),
        new SeedPermission(PermissionSeedId(33), PermissionCodes.AdminPermissionsManage, "Gestionar permisos", null),
        new SeedPermission(PermissionSeedId(34), PermissionCodes.ChatMessage, "Enviar mensaje de chat", null),
        new SeedPermission(PermissionSeedId(35), PermissionCodes.ChatLogsRead, "Ver historial de chat", null),
        new SeedPermission(PermissionSeedId(36), PermissionCodes.ReportsSalesRead, "Ver reporte de ventas", null),
        new SeedPermission(PermissionSeedId(37), PermissionCodes.ReportsInventoryRead, "Ver reporte de inventario", null)
    };

    public static IReadOnlyCollection<SeedUser> BootstrapUsers { get; } = new[]
    {
        new SeedUser(
            UserSeedId(1),
            "Admin Bootstrap",
            "admin.bootstrap@bibliobot.test",
            "Admin_Bootstrap_123!",
            true)
    };

    private static Guid RoleSeedId(int number)
    {
        return new Guid($"10000000-0000-0000-0000-{number:000000000000}");
    }

    private static Guid PermissionSeedId(int number)
    {
        return new Guid($"20000000-0000-0000-0000-{number:000000000000}");
    }

    private static Guid UserSeedId(int number)
    {
        return new Guid($"90000000-0000-0000-0000-{number:000000000000}");
    }
}
