using Application.Common.Interfaces;
using Application.Features.Books.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Books.UpdateBook;

public sealed class UpdateBookCommandHandler : IRequestHandler<UpdateBookCommand, BookDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateBookCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BookDetailDto?> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _context.Books
            .Include(current => current.BookAuthors)
            .Include(current => current.BookCategories)
            .Include(current => current.Publisher)
            .Include(current => current.InventoryStocks)
            .ThenInclude(stock => stock.Branch)
            .Where(current => current.Id == request.Id && !current.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);

        if (book is null)
        {
            return null;
        }

        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) || title.Length > 250)
        {
            throw new ArgumentException("El título es obligatorio y debe tener máximo 250 caracteres.");
        }

        var isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : request.Isbn.Trim();
        if (isbn is not null && isbn.Length > 30)
        {
            throw new ArgumentException("El ISBN debe tener máximo 30 caracteres.");
        }

        var language = string.IsNullOrWhiteSpace(request.Language) ? null : request.Language.Trim();
        if (language is not null && language.Length > 50)
        {
            throw new ArgumentException("El idioma debe tener máximo 50 caracteres.");
        }

        if (request.PublicationYear is < 1)
        {
            throw new ArgumentException("El año de publicación debe ser mayor a 0.");
        }

        if (request.Price < 0)
        {
            throw new ArgumentException("El precio debe ser mayor o igual a 0.");
        }

        var description = request.Description?.Trim();
        var imageUrl = request.ImageUrl?.Trim();
        var authorIds = GetDistinctIds(request.AuthorIds);
        var categoryIds = GetDistinctIds(request.CategoryIds);

        if (isbn is not null)
        {
            var isbnExists = await _context.Books.AnyAsync(
                current => !current.IsDeleted
                    && current.Id != request.Id
                    && current.Isbn != null
                    && current.Isbn == isbn,
                cancellationToken);
            if (isbnExists)
            {
                throw new InvalidOperationException("Ya existe un libro con ese ISBN.");
            }
        }

        string? publisherName = book.Publisher?.Name;
        if (request.PublisherId is not null)
        {
            var publisher = await _context.Publishers.FirstOrDefaultAsync(
                current => current.Id == request.PublisherId.Value && current.IsActive,
                cancellationToken);
            if (publisher is null)
            {
                throw new KeyNotFoundException("La editorial especificada no existe.");
            }

            publisherName = publisher.Name;
        }

        var authors = await _context.Authors
            .Where(author => authorIds.Contains(author.Id))
            .ToListAsync(cancellationToken);
        if (authors.Count != authorIds.Count)
        {
            throw new KeyNotFoundException("Al menos un autor no existe.");
        }

        var categories = await _context.Categories
            .Where(category => categoryIds.Contains(category.Id))
            .ToListAsync(cancellationToken);
        if (categories.Count != categoryIds.Count)
        {
            throw new KeyNotFoundException("Al menos una categoría no existe.");
        }

        _context.BookAuthors.RemoveRange(book.BookAuthors);
        _context.BookCategories.RemoveRange(book.BookCategories);

        book.Title = title;
        book.Isbn = isbn;
        book.Description = description;
        book.PublisherId = request.PublisherId;
        book.PublicationYear = request.PublicationYear;
        book.Language = language;
        book.ImageUrl = imageUrl;
        book.Price = request.Price;
        book.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var authorId in authorIds)
        {
            _context.BookAuthors.Add(new BookAuthor { BookId = book.Id, AuthorId = authorId });
        }

        foreach (var categoryId in categoryIds)
        {
            _context.BookCategories.Add(new BookCategory { BookId = book.Id, CategoryId = categoryId });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new BookDetailDto
        {
            Id = book.Id,
            Title = book.Title,
            Isbn = book.Isbn,
            Description = book.Description,
            PublisherName = publisherName,
            PublicationYear = book.PublicationYear,
            Language = book.Language,
            ImageUrl = book.ImageUrl,
            Price = book.Price,
            Authors = authors.Select(author => author.FullName).Distinct().ToList(),
            Categories = categories.Select(category => category.Name).Distinct().ToList(),
            TotalStock = book.InventoryStocks.Sum(stock => stock.CurrentStock),
            StockByBranch = book.InventoryStocks
                .Select(stock => new BookBranchStockDto
                {
                    BranchId = stock.BranchId,
                    BranchName = stock.Branch.Name,
                    CurrentStock = stock.CurrentStock,
                })
                .Distinct()
                .ToList()
        };
    }

    private static IReadOnlyCollection<Guid> GetDistinctIds(IReadOnlyCollection<Guid>? ids)
    {
        if (ids == null || ids.Count == 0)
        {
            return [];
        }

        return ids.Distinct().ToList();
    }
}
