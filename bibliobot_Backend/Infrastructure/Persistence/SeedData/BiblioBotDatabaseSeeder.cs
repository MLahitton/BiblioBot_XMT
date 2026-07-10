using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Application.Common.Interfaces;
using Application.Common.Text;
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

        var categoryNamesToSeed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var categoryName in CategoryTaxonomySeedData.CategoryNames)
        {
            if (!categoryNamesToSeed.Add(categoryName))
            {
                continue;
            }

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

        await _dbContext.SaveChangesAsync(cancellationToken);
        await SeedRealCatalogAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SeedInitialInventoryStockAsync(cancellationToken);

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

    private async Task SeedRealCatalogAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var authorsByName = (await _dbContext.Authors.ToListAsync(cancellationToken))
            .GroupBy(author => NormalizeKey(author.FullName))
            .ToDictionary(group => group.Key, group => group.First());

        var categoriesByName = (await _dbContext.Categories.ToListAsync(cancellationToken))
            .GroupBy(category => NormalizeKey(category.Name))
            .ToDictionary(group => group.Key, group => group.First());

        var publishersByName = (await _dbContext.Publishers.ToListAsync(cancellationToken))
            .GroupBy(publisher => NormalizeKey(publisher.Name))
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var categoryName in RealCatalogSeedData.Categories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            EnsureCategory(categoryName, categoriesByName, now);
        }

        var publisherNames = RealCatalogSeedData.Publishers
            .Concat(RealCatalogSeedData.Books.Select(book => book.Publisher).Where(publisher => !string.IsNullOrWhiteSpace(publisher))!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var publisherName in publisherNames)
        {
            EnsurePublisher(publisherName!, publishersByName, now);
        }

        foreach (var authorName in RealCatalogSeedData.Authors)
        {
            EnsureAuthor(authorName, authorsByName, now);
        }

        var books = await _dbContext.Books
            .Include(book => book.BookAuthors)
                .ThenInclude(bookAuthor => bookAuthor.Author)
            .Include(book => book.BookCategories)
            .ToListAsync(cancellationToken);

        var bookAuthorPairs = books
            .SelectMany(book => book.BookAuthors.Select(bookAuthor => (bookAuthor.BookId, bookAuthor.AuthorId)))
            .ToHashSet();

        var bookCategoryPairs = books
            .SelectMany(book => book.BookCategories.Select(bookCategory => (bookCategory.BookId, bookCategory.CategoryId)))
            .ToHashSet();

        foreach (var seedBook in RealCatalogSeedData.Books)
        {
            var primaryAuthorName = seedBook.Authors.First();
            var book = books.FirstOrDefault(existingBook =>
                SameText(existingBook.Title, seedBook.Title) &&
                existingBook.BookAuthors.Any(bookAuthor => SameText(bookAuthor.Author.FullName, primaryAuthorName)));

            var publisher = string.IsNullOrWhiteSpace(seedBook.Publisher)
                ? null
                : EnsurePublisher(seedBook.Publisher, publishersByName, now);

            if (book is null)
            {
                book = new Book
                {
                    Title = seedBook.Title,
                    Isbn = null,
                    Description = seedBook.Description,
                    PublisherId = publisher?.Id,
                    PublicationYear = seedBook.PublicationYear,
                    Language = seedBook.Language,
                    ImageUrl = null,
                    Price = seedBook.Price,
                    IsActive = true,
                    IsDeleted = false,
                };

                _dbContext.Books.Add(book);
                books.Add(book);
            }
            else
            {
                UpdateExistingBook(book, seedBook, publisher, now);
            }

            foreach (var authorName in seedBook.Authors)
            {
                var author = EnsureAuthor(authorName, authorsByName, now);
                var pair = (book.Id, author.Id);

                if (bookAuthorPairs.Add(pair))
                {
                    _dbContext.BookAuthors.Add(new BookAuthor
                    {
                        BookId = book.Id,
                        AuthorId = author.Id,
                    });
                }
            }

            foreach (var categoryName in seedBook.Categories)
            {
                var category = EnsureCategory(categoryName, categoriesByName, now);
                var pair = (book.Id, category.Id);

                if (bookCategoryPairs.Add(pair))
                {
                    _dbContext.BookCategories.Add(new BookCategory
                    {
                        BookId = book.Id,
                        CategoryId = category.Id,
                    });
                }
            }
        }
    }

    private async Task SeedInitialInventoryStockAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var branches = await _dbContext.Branches
            .Where(branch => branch.IsActive)
            .OrderBy(branch => branch.Name)
            .ToListAsync(cancellationToken);

        if (branches.Count == 0)
        {
            var centralBranch = new Branch
            {
                Name = "Sede Central",
                Address = "Sede principal de BiblioBot",
                IsActive = true,
            };

            _dbContext.Branches.Add(centralBranch);
            branches.Add(centralBranch);
        }

        var stockBranches = branches
            .Take(Math.Min(3, branches.Count))
            .ToList();

        var books = await _dbContext.Books
            .Include(book => book.BookCategories)
                .ThenInclude(bookCategory => bookCategory.Category)
            .Include(book => book.InventoryStocks)
            .Where(book => book.IsActive && !book.IsDeleted)
            .ToListAsync(cancellationToken);

        var booksByTitle = books
            .GroupBy(book => NormalizeKey(book.Title))
            .ToDictionary(group => group.Key, group => group.First());

        foreach (var seedBook in RealCatalogSeedData.Books)
        {
            var bookKey = NormalizeKey(seedBook.Title);

            if (!booksByTitle.TryGetValue(bookKey, out var book))
            {
                continue;
            }

            var targetStock = ResolveInitialStock(seedBook);
            var minimumStock = ResolveMinimumStock(targetStock);

            foreach (var (branch, quantity) in BuildStockAllocation(stockBranches, targetStock))
            {
                var existingStock = book.InventoryStocks.FirstOrDefault(stock => stock.BranchId == branch.Id);

                if (existingStock is null)
                {
                    _dbContext.InventoryStocks.Add(new InventoryStock
                    {
                        BookId = book.Id,
                        BranchId = branch.Id,
                        CurrentStock = quantity,
                        MinStock = minimumStock,
                    });

                    continue;
                }

                var wasUpdated = false;

                if (existingStock.CurrentStock <= 0)
                {
                    existingStock.CurrentStock = quantity;
                    wasUpdated = true;
                }

                if (existingStock.MinStock <= 0)
                {
                    existingStock.MinStock = minimumStock;
                    wasUpdated = true;
                }

                if (wasUpdated)
                {
                    existingStock.UpdatedAt = now;
                }
            }
        }
    }

    private static int ResolveInitialStock(RealCatalogBookSeed seedBook)
    {
        var title = NormalizeKey(seedBook.Title);
        var categories = seedBook.Categories
            .Select(NormalizeKey)
            .ToList();

        if (title.Contains("harry potter", StringComparison.Ordinal))
        {
            return 18;
        }

        if (title.Contains("el hobbit", StringComparison.Ordinal))
        {
            return 15;
        }

        if (title.Contains("el senor de los anillos", StringComparison.Ordinal))
        {
            return 12;
        }

        if (title.Contains("clean code", StringComparison.Ordinal))
        {
            return 8;
        }

        if (title.Contains("cien anos de soledad", StringComparison.Ordinal))
        {
            return 14;
        }

        if (categories.Any(category => category.Contains("fantasia", StringComparison.Ordinal)
            || category.Contains("juvenil", StringComparison.Ordinal)
            || category.Contains("infantil", StringComparison.Ordinal)))
        {
            return 14;
        }

        if (categories.Any(category => category.Contains("programacion", StringComparison.Ordinal)
            || category.Contains("ingenieria", StringComparison.Ordinal)
            || category.Contains("arquitectura", StringComparison.Ordinal)
            || category.Contains("algoritmos", StringComparison.Ordinal)
            || category.Contains("tecnologia", StringComparison.Ordinal)))
        {
            return 8;
        }

        if (categories.Any(category => category.Contains("literatura clasica", StringComparison.Ordinal)
            || category.Contains("novela", StringComparison.Ordinal)))
        {
            return 10;
        }

        if (categories.Any(category => category.Contains("psicologia", StringComparison.Ordinal)
            || category.Contains("desarrollo", StringComparison.Ordinal)
            || category.Contains("filosofia", StringComparison.Ordinal)
            || category.Contains("ensayo", StringComparison.Ordinal)))
        {
            return 8;
        }

        return 10;
    }

    private static int ResolveMinimumStock(int targetStock)
    {
        return Math.Max(2, targetStock / 4);
    }

    private static IReadOnlyCollection<(Branch Branch, int Quantity)> BuildStockAllocation(
        IReadOnlyList<Branch> branches,
        int targetStock)
    {
        if (branches.Count == 0)
        {
            return [];
        }

        if (branches.Count == 1)
        {
            return [(branches[0], targetStock)];
        }

        if (branches.Count == 2)
        {
            var firstQuantity = Math.Max(1, targetStock * 60 / 100);
            return [(branches[0], firstQuantity), (branches[1], targetStock - firstQuantity)];
        }

        var centralQuantity = Math.Max(1, targetStock * 50 / 100);
        var secondaryQuantity = Math.Max(1, targetStock * 30 / 100);
        var tertiaryQuantity = Math.Max(1, targetStock - centralQuantity - secondaryQuantity);

        return [(branches[0], centralQuantity), (branches[1], secondaryQuantity), (branches[2], tertiaryQuantity)];
    }
    private Author EnsureAuthor(
        string authorName,
        IDictionary<string, Author> authorsByName,
        DateTimeOffset now)
    {
        var key = NormalizeKey(authorName);

        if (authorsByName.TryGetValue(key, out var author))
        {
            if (!author.IsActive)
            {
                author.IsActive = true;
                author.UpdatedAt = now;
            }

            return author;
        }

        author = new Author
        {
            FullName = authorName.Trim(),
            IsActive = true,
        };

        _dbContext.Authors.Add(author);
        authorsByName[key] = author;

        return author;
    }

    private Category EnsureCategory(
        string categoryName,
        IDictionary<string, Category> categoriesByName,
        DateTimeOffset now)
    {
        var key = NormalizeKey(categoryName);

        if (categoriesByName.TryGetValue(key, out var category))
        {
            if (!category.IsActive)
            {
                category.IsActive = true;
                category.UpdatedAt = now;
            }

            return category;
        }

        category = new Category
        {
            Name = categoryName.Trim(),
            IsActive = true,
        };

        _dbContext.Categories.Add(category);
        categoriesByName[key] = category;

        return category;
    }

    private Publisher EnsurePublisher(
        string publisherName,
        IDictionary<string, Publisher> publishersByName,
        DateTimeOffset now)
    {
        var key = NormalizeKey(publisherName);

        if (publishersByName.TryGetValue(key, out var publisher))
        {
            if (!publisher.IsActive)
            {
                publisher.IsActive = true;
                publisher.UpdatedAt = now;
            }

            return publisher;
        }

        publisher = new Publisher
        {
            Name = publisherName.Trim(),
            IsActive = true,
        };

        _dbContext.Publishers.Add(publisher);
        publishersByName[key] = publisher;

        return publisher;
    }

    private static void UpdateExistingBook(
        Book book,
        RealCatalogBookSeed seedBook,
        Publisher? publisher,
        DateTimeOffset now)
    {
        var wasUpdated = false;

        if (book.IsDeleted)
        {
            book.IsDeleted = false;
            book.DeletedAt = null;
            wasUpdated = true;
        }

        if (!book.IsActive)
        {
            book.IsActive = true;
            wasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(book.Description))
        {
            book.Description = seedBook.Description;
            wasUpdated = true;
        }

        if (book.PublisherId is null && publisher is not null)
        {
            book.PublisherId = publisher.Id;
            wasUpdated = true;
        }

        if (book.PublicationYear is null && seedBook.PublicationYear is not null)
        {
            book.PublicationYear = seedBook.PublicationYear;
            wasUpdated = true;
        }

        if (string.IsNullOrWhiteSpace(book.Language))
        {
            book.Language = seedBook.Language;
            wasUpdated = true;
        }

        if (book.Price <= 0)
        {
            book.Price = seedBook.Price;
            wasUpdated = true;
        }

        if (wasUpdated)
        {
            book.UpdatedAt = now;
        }
    }

    private static bool SameText(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeKey(string value)
    {
        return TextSearchNormalizer.Normalize(value);
    }
}





