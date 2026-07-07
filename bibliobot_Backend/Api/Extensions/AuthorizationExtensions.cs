using Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Application.Common.Security;

namespace Api.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(PermissionCodes.AuthMe, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.AuthMe)));

            options.AddPolicy(PermissionCodes.AuthLogout, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.AuthLogout)));

            options.AddPolicy(PermissionCodes.AuthChangePassword, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.AuthChangePassword)));

            options.AddPolicy(PermissionCodes.BooksRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.BooksRead)));

            options.AddPolicy(PermissionCodes.BooksSearch, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.BooksSearch)));

            options.AddPolicy(PermissionCodes.BooksCreate, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.BooksCreate)));

            options.AddPolicy(PermissionCodes.BooksUpdate, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.BooksUpdate)));

            options.AddPolicy(PermissionCodes.BooksDisable, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.BooksDisable)));

            options.AddPolicy(PermissionCodes.BooksActivate, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.BooksActivate)));

            options.AddPolicy(PermissionCodes.CartManage, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.CartManage)));

            options.AddPolicy(PermissionCodes.CartRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.CartRead)));

            options.AddPolicy(PermissionCodes.SalesCreate, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.SalesCreate)));

            options.AddPolicy(PermissionCodes.SalesConfirm, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.SalesConfirm)));

            options.AddPolicy(PermissionCodes.SalesReadOwn, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.SalesReadOwn)));

            options.AddPolicy(PermissionCodes.SalesReadAll, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.SalesReadAll)));

            options.AddPolicy(PermissionCodes.SalesCancel, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.SalesCancel)));

            options.AddPolicy(PermissionCodes.InvoicesReadOwn, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.InvoicesReadOwn)));

            options.AddPolicy(PermissionCodes.InvoicesReadAll, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.InvoicesReadAll)));

            options.AddPolicy(PermissionCodes.InventoryRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.InventoryRead)));

            options.AddPolicy(PermissionCodes.InventoryEntry, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.InventoryEntry)));

            options.AddPolicy(PermissionCodes.InventoryExit, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.InventoryExit)));

            options.AddPolicy(PermissionCodes.InventoryAdjust, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.InventoryAdjust)));

            options.AddPolicy(PermissionCodes.RequestsPurchaseCreate, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.RequestsPurchaseCreate)));

            options.AddPolicy(PermissionCodes.RequestsTransferCreate, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.RequestsTransferCreate)));

            options.AddPolicy(PermissionCodes.RequestsRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.RequestsRead)));

            options.AddPolicy(PermissionCodes.RequestsReview, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.RequestsReview)));

            options.AddPolicy(PermissionCodes.RequestsApprove, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.RequestsApprove)));

            options.AddPolicy(PermissionCodes.RequestsReject, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.RequestsReject)));

            options.AddPolicy(PermissionCodes.RequestsExecute, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.RequestsExecute)));

            options.AddPolicy(PermissionCodes.AdminUsersRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.AdminUsersRead)));

            options.AddPolicy(PermissionCodes.AdminRolesRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.AdminRolesRead)));

            options.AddPolicy(PermissionCodes.AdminPermissionsRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.AdminPermissionsRead)));

            options.AddPolicy(PermissionCodes.AdminPermissionsManage, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.AdminPermissionsManage)));

            options.AddPolicy(PermissionCodes.ChatMessage, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.ChatMessage)));

            options.AddPolicy(PermissionCodes.ChatLogsRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.ChatLogsRead)));

            options.AddPolicy(PermissionCodes.ReportsSalesRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.ReportsSalesRead)));

            options.AddPolicy(PermissionCodes.ReportsInventoryRead, policy =>
                policy.Requirements.Add(new PermissionRequirement(PermissionCodes.ReportsInventoryRead)));
        });

        return services;
    }
}
