using System.Collections.Generic;
using Domain.Constants;

namespace Infrastructure.Persistence.SeedData;

public static class CatalogSeedData
{
    public static IReadOnlyCollection<SeedSaleStatus> SaleStatuses { get; } = new[]
    {
        new SeedSaleStatus(SeedId(40000000, 1), SaleStatusCodes.Created, "Creada"),
        new SeedSaleStatus(SeedId(40000000, 2), SaleStatusCodes.PendingConfirmation, "Pendiente de confirmación"),
        new SeedSaleStatus(SeedId(40000000, 3), SaleStatusCodes.Confirmed, "Confirmada"),
        new SeedSaleStatus(SeedId(40000000, 4), SaleStatusCodes.Rejected, "Rechazada"),
        new SeedSaleStatus(SeedId(40000000, 5), SaleStatusCodes.Cancelled, "Cancelada")
    };

    public static IReadOnlyCollection<SeedSaleOrigin> SaleOrigins { get; } = new[]
    {
        new SeedSaleOrigin(SeedId(50000000, 1), SaleOriginCodes.WebUi, "Interfaz web"),
        new SeedSaleOrigin(SeedId(50000000, 2), SaleOriginCodes.Chatbot, "Chatbot")
    };

    public static IReadOnlyCollection<SeedInventoryMovementType> InventoryMovementTypes { get; } = new[]
    {
        new SeedInventoryMovementType(SeedId(60000000, 1), InventoryMovementTypeCodes.Entry, "Entrada"),
        new SeedInventoryMovementType(SeedId(60000000, 2), InventoryMovementTypeCodes.Exit, "Salida"),
        new SeedInventoryMovementType(SeedId(60000000, 3), InventoryMovementTypeCodes.Adjustment, "Ajuste"),
        new SeedInventoryMovementType(SeedId(60000000, 4), InventoryMovementTypeCodes.Sale, "Venta"),
        new SeedInventoryMovementType(SeedId(60000000, 5), InventoryMovementTypeCodes.TransferIn, "Traslado entrada"),
        new SeedInventoryMovementType(SeedId(60000000, 6), InventoryMovementTypeCodes.TransferOut, "Traslado salida")
    };

    public static IReadOnlyCollection<SeedRequestType> RequestTypes { get; } = new[]
    {
        new SeedRequestType(SeedId(70000000, 1), RequestTypeCodes.Purchase, "Compra"),
        new SeedRequestType(SeedId(70000000, 2), RequestTypeCodes.Transfer, "Traslado")
    };

    public static IReadOnlyCollection<SeedRequestStatus> RequestStatuses { get; } = new[]
    {
        new SeedRequestStatus(SeedId(80000000, 1), RequestStatusCodes.Created, "Creado"),
        new SeedRequestStatus(SeedId(80000000, 2), RequestStatusCodes.InReview, "En revisión"),
        new SeedRequestStatus(SeedId(80000000, 3), RequestStatusCodes.Approved, "Aprobado"),
        new SeedRequestStatus(SeedId(80000000, 4), RequestStatusCodes.Rejected, "Rechazado"),
        new SeedRequestStatus(SeedId(80000000, 5), RequestStatusCodes.Executed, "Ejecutado")
    };

    private static Guid SeedId(int prefix, int number)
    {
        return new Guid($"{prefix}-0000-0000-0000-{number:000000000000}");
    }
}
