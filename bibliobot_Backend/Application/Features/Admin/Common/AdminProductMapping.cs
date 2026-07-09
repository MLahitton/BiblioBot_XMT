using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.Common;

internal static class AdminProductMapping
{
    public static AdminProductDto ToDto(Book book)
    {
        var stock = book.InventoryStocks
            .OrderBy(current => current.Branch.Name)
            .FirstOrDefault();

        return new AdminProductDto
        {
            Id = book.Id,
            Title = book.Title,
            Isbn = book.Isbn,
            Description = book.Description,
            PublisherName = book.Publisher?.Name,
            PublicationYear = book.PublicationYear,
            Language = book.Language,
            ImageUrl = book.ImageUrl,
            Price = book.Price,
            IsActive = book.IsActive,
            Authors = book.BookAuthors
                .Select(author => author.Author.FullName)
                .Distinct()
                .OrderBy(name => name)
                .ToList(),
            Categories = book.BookCategories
                .Select(category => category.Category.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList(),
            BranchId = stock?.BranchId,
            BranchName = stock?.Branch.Name,
            CurrentStock = stock?.CurrentStock ?? 0,
            MinStock = stock?.MinStock ?? 0,
            PurchasedCount = book.SaleDetails.Sum(detail => detail.Quantity),
            FavoriteCount = book.UserFavoriteBooks.Count,
            CreatedAt = book.CreatedAt,
            UpdatedAt = book.UpdatedAt,
        };
    }

    public static void Validate(AdminProductMutation request)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) || title.Length > 250)
        {
            throw new ArgumentException("El titulo es obligatorio y debe tener maximo 250 caracteres.");
        }

        var isbn = request.Isbn?.Trim();
        if (!string.IsNullOrWhiteSpace(isbn) && isbn.Length > 30)
        {
            throw new ArgumentException("El ISBN debe tener maximo 30 caracteres.");
        }

        var language = request.Language?.Trim();
        if (!string.IsNullOrWhiteSpace(language) && language.Length > 50)
        {
            throw new ArgumentException("El idioma debe tener maximo 50 caracteres.");
        }

        var publisherName = request.PublisherName?.Trim();
        if (!string.IsNullOrWhiteSpace(publisherName) && publisherName.Length > 160)
        {
            throw new ArgumentException("La editorial debe tener maximo 160 caracteres.");
        }

        if (request.PublicationYear is < 1)
        {
            throw new ArgumentException("El anio de publicacion debe ser mayor a 0.");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException("El precio debe ser mayor o igual a 0.");
        }

        if (request.CurrentStock < 0)
        {
            throw new ArgumentException("El stock debe ser mayor o igual a 0.");
        }

        if (request.MinStock < 0)
        {
            throw new ArgumentException("El stock minimo debe ser mayor o igual a 0.");
        }

        foreach (var authorName in NormalizeNames(request.AuthorNames))
        {
            if (authorName.Length > 160)
            {
                throw new ArgumentException("Cada autor debe tener maximo 160 caracteres.");
            }
        }

        foreach (var categoryName in NormalizeNames(request.CategoryNames))
        {
            if (categoryName.Length > 120)
            {
                throw new ArgumentException("Cada categoria debe tener maximo 120 caracteres.");
            }
        }
    }

    public static async Task<Publisher?> ResolvePublisherAsync(
        IApplicationDbContext context,
        string? name,
        CancellationToken cancellationToken)
    {
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        var loweredName = normalizedName.ToLowerInvariant();
        var publisher = await context.Publishers.FirstOrDefaultAsync(
            current => current.Name.ToLower() == loweredName,
            cancellationToken);

        if (publisher is not null)
        {
            publisher.IsActive = true;
            return publisher;
        }

        publisher = new Publisher
        {
            Name = normalizedName,
            IsActive = true,
        };

        context.Publishers.Add(publisher);
        return publisher;
    }

    public static async Task<IReadOnlyCollection<Author>> ResolveAuthorsAsync(
        IApplicationDbContext context,
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken)
    {
        var authors = new List<Author>();

        foreach (var name in NormalizeNames(names))
        {
            var loweredName = name.ToLowerInvariant();
            var author = await context.Authors.FirstOrDefaultAsync(
                current => current.FullName.ToLower() == loweredName,
                cancellationToken);

            if (author is null)
            {
                author = new Author
                {
                    FullName = name,
                    IsActive = true,
                };

                context.Authors.Add(author);
            }
            else
            {
                author.IsActive = true;
            }

            authors.Add(author);
        }

        return authors;
    }

    public static async Task<IReadOnlyCollection<Category>> ResolveCategoriesAsync(
        IApplicationDbContext context,
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken)
    {
        var categories = new List<Category>();
        var normalizedNames = NormalizeNames(names);

        if (normalizedNames.Count == 0)
        {
            throw new ArgumentException("Selecciona una categoria principal.");
        }

        foreach (var name in normalizedNames)
        {
            var loweredName = name.ToLowerInvariant();
            var category = await context.Categories.FirstOrDefaultAsync(
                current => current.IsActive && current.Name.ToLower() == loweredName,
                cancellationToken);

            if (category is null)
            {
                throw new InvalidOperationException("La categoria seleccionada no esta disponible.");
            }

            categories.Add(category);
        }

        return categories;
    }

    public static async Task<Branch> ResolveBranchAsync(
        IApplicationDbContext context,
        Guid? branchId,
        CancellationToken cancellationToken)
    {
        if (branchId.HasValue)
        {
            var branch = await context.Branches.FirstOrDefaultAsync(
                current => current.Id == branchId.Value && current.IsActive,
                cancellationToken);

            if (branch is null)
            {
                throw new KeyNotFoundException("La sede especificada no existe.");
            }

            return branch;
        }

        var defaultBranch = await context.Branches
            .OrderBy(branch => branch.Name)
            .FirstOrDefaultAsync(branch => branch.IsActive, cancellationToken);

        if (defaultBranch is not null)
        {
            return defaultBranch;
        }

        defaultBranch = new Branch
        {
            Name = "Sede principal",
            IsActive = true,
        };

        context.Branches.Add(defaultBranch);
        return defaultBranch;
    }

    public static IReadOnlyCollection<string> NormalizeNames(IReadOnlyCollection<string>? names)
    {
        if (names is null)
        {
            return [];
        }

        return names
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
