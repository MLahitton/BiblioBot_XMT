namespace Domain.Constants;

public static class PermissionCodes
{
    public const string AuthMe = "auth.me";
    public const string AuthLogout = "auth.logout";
    public const string AuthChangePassword = "auth.change_password";

    public const string BooksRead = "books.read";
    public const string BooksSearch = "books.search";
    public const string BooksCreate = "books.create";
    public const string BooksUpdate = "books.update";
    public const string BooksDisable = "books.disable";
    public const string BooksActivate = "books.activate";

    public const string CartManage = "cart.manage";
    public const string CartRead = "cart.read";

    public const string SalesCreate = "sales.create";
    public const string SalesConfirm = "sales.confirm";
    public const string SalesReadOwn = "sales.read_own";
    public const string SalesReadAll = "sales.read_all";
    public const string SalesCancel = "sales.cancel";

    public const string InvoicesReadOwn = "invoices.read_own";
    public const string InvoicesReadAll = "invoices.read_all";

    public const string InventoryRead = "inventory.read";
    public const string InventoryEntry = "inventory.entry";
    public const string InventoryExit = "inventory.exit";
    public const string InventoryAdjust = "inventory.adjust";

    public const string RequestsPurchaseCreate = "requests.purchase.create";
    public const string RequestsTransferCreate = "requests.transfer.create";
    public const string RequestsRead = "requests.read";
    public const string RequestsReview = "requests.review";
    public const string RequestsApprove = "requests.approve";
    public const string RequestsReject = "requests.reject";
    public const string RequestsExecute = "requests.execute";

    public const string AdminUsersRead = "admin.users.read";
    public const string AdminRolesRead = "admin.roles.read";
    public const string AdminPermissionsRead = "admin.permissions.read";
    public const string AdminPermissionsManage = "admin.permissions.manage";

    public const string ChatMessage = "chat.message";
    public const string ChatLogsRead = "chat.logs.read";

    public const string ReportsSalesRead = "reports.sales.read";
    public const string ReportsInventoryRead = "reports.inventory.read";
}
