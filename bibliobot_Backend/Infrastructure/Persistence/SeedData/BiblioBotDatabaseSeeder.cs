using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Application.Common.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SeedData;

public class BiblioBotDatabaseSeeder : IDatabaseSeeder
{
    private readonly BiblioBotDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public BiblioBotDatabaseSeeder(BiblioBotDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var adminRoleSeed = AuthSeedData.Roles.First(role => role.Code == RoleCodes.Admin);

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

        await _dbContext.SaveChangesAsync(cancellationToken);

        var adminRole = await _dbContext.Roles
            .FirstOrDefaultAsync(role => role.Code == adminRoleSeed.Code, cancellationToken);

        if (adminRole is not null)
        {
            var adminPermissionIds = await _dbContext.RolePermissions
                .Where(rolePermission => rolePermission.RoleId == adminRole.Id)
                .Select(rolePermission => rolePermission.PermissionId)
                .ToListAsync(cancellationToken);

            var adminPermissionsSet = new HashSet<Guid>(adminPermissionIds);

            foreach (var seedPermission in AuthSeedData.Permissions)
            {
                var permission = await _dbContext.Permissions.FirstOrDefaultAsync(
                    permission => permission.Code == seedPermission.Code,
                    cancellationToken);

                if (permission is null)
                {
                    continue;
                }

                if (!adminPermissionsSet.Contains(permission.Id))
                {
                    _dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        PermissionId = permission.Id,
                    });

                    adminPermissionsSet.Add(permission.Id);
                }
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

        foreach (var (oldName, newName) in CategoryTaxonomySeedData.CategoryRenames)
        {
            var existingCategory = await _dbContext.Categories.FirstOrDefaultAsync(
                category => category.Name.ToLower() == oldName.ToLower(),
                cancellationToken);

            if (existingCategory is null)
            {
                continue;
            }

            var targetCategory = await _dbContext.Categories.FirstOrDefaultAsync(
                category => category.Id != existingCategory.Id && category.Name.ToLower() == newName.ToLower(),
                cancellationToken);

            if (targetCategory is not null)
            {
                var oldBookCategories = await _dbContext.BookCategories
                    .Where(bookCategory => bookCategory.CategoryId == existingCategory.Id)
                    .ToListAsync(cancellationToken);
                var targetBookIds = await _dbContext.BookCategories
                    .Where(bookCategory => bookCategory.CategoryId == targetCategory.Id)
                    .Select(bookCategory => bookCategory.BookId)
                    .ToListAsync(cancellationToken);
                var targetBookIdSet = new HashSet<Guid>(targetBookIds);

                foreach (var oldBookCategory in oldBookCategories)
                {
                    if (targetBookIdSet.Contains(oldBookCategory.BookId))
                    {
                        continue;
                    }

                    _dbContext.BookCategories.Add(new BookCategory
                    {
                        BookId = oldBookCategory.BookId,
                        CategoryId = targetCategory.Id,
                    });
                }

                _dbContext.BookCategories.RemoveRange(oldBookCategories);
                existingCategory.IsActive = false;
                existingCategory.UpdatedAt = DateTimeOffset.UtcNow;
                continue;
            }

            existingCategory.Name = newName;
            existingCategory.IsActive = true;
            existingCategory.UpdatedAt = DateTimeOffset.UtcNow;
        }

        foreach (var categoryName in CategoryTaxonomySeedData.CategoryNames)
        {
            var exists = await _dbContext.Categories.AnyAsync(
                category => category.Name.ToLower() == categoryName.ToLower(),
                cancellationToken);

            if (!exists)
            {
                _dbContext.Categories.Add(new Category
                {
                    Name = categoryName,
                    IsActive = true,
                });
            }
        }

        adminRole = await _dbContext.Roles
            .FirstOrDefaultAsync(role => role.Code == adminRoleSeed.Code && role.IsActive, cancellationToken);

        var officialAdminSeed = AuthSeedData.BootstrapUsers
            .FirstOrDefault(user => user.RoleCode == RoleCodes.Admin);

        foreach (var seedUser in AuthSeedData.BootstrapUsers)
        {
            var email = seedUser.Email.Trim().ToLowerInvariant();
            var user = await _dbContext.Users.FirstOrDefaultAsync(
                existingUser => existingUser.Id == seedUser.Id || existingUser.Email == email,
                cancellationToken);

            if (user is null)
            {
                user = new User
                {
                    Id = seedUser.Id,
                    FullName = seedUser.FullName,
                    Email = email,
                    PasswordHash = string.Empty,
                    IsActive = seedUser.IsActive,
                };

                user.PasswordHash = _passwordHasher.HashPassword(user, seedUser.TempPassword);
                _dbContext.Users.Add(user);
            }
            else
            {
                user.FullName = seedUser.FullName;
                user.Email = email;
                user.IsActive = seedUser.IsActive;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, seedUser.TempPassword);

            var seedRole = await _dbContext.Roles
                .FirstOrDefaultAsync(role => role.Code == seedUser.RoleCode && role.IsActive, cancellationToken);

            if (seedRole is not null)
            {
                var hasAdminRole = await _dbContext.UserRoles.AnyAsync(
                    userRole => userRole.UserId == user.Id && userRole.RoleId == seedRole.Id,
                    cancellationToken);

                if (!hasAdminRole)
                {
                    _dbContext.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = seedRole.Id,
                        CreatedAt = DateTimeOffset.UtcNow,
                    });
                }
            }
        }

        if (adminRole is not null && officialAdminSeed is not null)
        {
            var nonOfficialAdminRoles = await _dbContext.UserRoles
                .Where(userRole =>
                    userRole.RoleId == adminRole.Id &&
                    userRole.UserId != officialAdminSeed.Id)
                .ToListAsync(cancellationToken);

            _dbContext.UserRoles.RemoveRange(nonOfficialAdminRoles);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
