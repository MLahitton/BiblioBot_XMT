using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SeedData;

public class BiblioBotDatabaseSeeder : IDatabaseSeeder
{
    private readonly BiblioBotDbContext _dbContext;

    public BiblioBotDatabaseSeeder(BiblioBotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var seedRole in AuthSeedData.Roles)
        {
            var exists = await _dbContext.Roles.AnyAsync(
                role => role.Id == seedRole.Id || role.Code == seedRole.Code,
                cancellationToken);

            if (!exists)
            {
                _dbContext.Roles.Add(new Role
                {
                    Id = seedRole.Id,
                    Code = seedRole.Code,
                    Name = seedRole.Name,
                    Description = seedRole.Description,
                    IsActive = true
                });
            }
        }

        foreach (var seedPermission in AuthSeedData.Permissions)
        {
            var exists = await _dbContext.Permissions.AnyAsync(
                permission => permission.Id == seedPermission.Id || permission.Code == seedPermission.Code,
                cancellationToken);

            if (!exists)
            {
                _dbContext.Permissions.Add(new Permission
                {
                    Id = seedPermission.Id,
                    Code = seedPermission.Code,
                    Name = seedPermission.Name,
                    Description = seedPermission.Description,
                    IsActive = true
                });
            }
        }

        foreach (var seedRolePermission in RolePermissionSeedData.RolePermissions)
        {
            var exists = await _dbContext.RolePermissions.AnyAsync(
                rolePermission =>
                    rolePermission.RoleId == seedRolePermission.RoleId &&
                    rolePermission.PermissionId == seedRolePermission.PermissionId,
                cancellationToken);

            if (!exists)
            {
                _dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = seedRolePermission.RoleId,
                    PermissionId = seedRolePermission.PermissionId
                });
            }
        }

        foreach (var seedSaleStatus in CatalogSeedData.SaleStatuses)
        {
            var exists = await _dbContext.SaleStatuses.AnyAsync(
                saleStatus => saleStatus.Id == seedSaleStatus.Id || saleStatus.Code == seedSaleStatus.Code,
                cancellationToken);

            if (!exists)
            {
                _dbContext.SaleStatuses.Add(new SaleStatus
                {
                    Id = seedSaleStatus.Id,
                    Code = seedSaleStatus.Code,
                    Name = seedSaleStatus.Name
                });
            }
        }

        foreach (var seedSaleOrigin in CatalogSeedData.SaleOrigins)
        {
            var exists = await _dbContext.SaleOrigins.AnyAsync(
                saleOrigin => saleOrigin.Id == seedSaleOrigin.Id || saleOrigin.Code == seedSaleOrigin.Code,
                cancellationToken);

            if (!exists)
            {
                _dbContext.SaleOrigins.Add(new SaleOrigin
                {
                    Id = seedSaleOrigin.Id,
                    Code = seedSaleOrigin.Code,
                    Name = seedSaleOrigin.Name
                });
            }
        }

        foreach (var seedInventoryMovementType in CatalogSeedData.InventoryMovementTypes)
        {
            var exists = await _dbContext.InventoryMovementTypes.AnyAsync(
                inventoryMovementType =>
                    inventoryMovementType.Id == seedInventoryMovementType.Id ||
                    inventoryMovementType.Code == seedInventoryMovementType.Code,
                cancellationToken);

            if (!exists)
            {
                _dbContext.InventoryMovementTypes.Add(new InventoryMovementType
                {
                    Id = seedInventoryMovementType.Id,
                    Code = seedInventoryMovementType.Code,
                    Name = seedInventoryMovementType.Name
                });
            }
        }

        foreach (var seedRequestType in CatalogSeedData.RequestTypes)
        {
            var exists = await _dbContext.RequestTypes.AnyAsync(
                requestType => requestType.Id == seedRequestType.Id || requestType.Code == seedRequestType.Code,
                cancellationToken);

            if (!exists)
            {
                _dbContext.RequestTypes.Add(new RequestType
                {
                    Id = seedRequestType.Id,
                    Code = seedRequestType.Code,
                    Name = seedRequestType.Name
                });
            }
        }

        foreach (var seedRequestStatus in CatalogSeedData.RequestStatuses)
        {
            var exists = await _dbContext.RequestStatuses.AnyAsync(
                requestStatus => requestStatus.Id == seedRequestStatus.Id || requestStatus.Code == seedRequestStatus.Code,
                cancellationToken);

            if (!exists)
            {
                _dbContext.RequestStatuses.Add(new RequestStatus
                {
                    Id = seedRequestStatus.Id,
                    Code = seedRequestStatus.Code,
                    Name = seedRequestStatus.Name
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
