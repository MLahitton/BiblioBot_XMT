using Application.Common.Interfaces;
using Application.Features.Books.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Books.CreateBook;

public sealed class CreateBookCommandHandler : IRequestHandler<CreateBookCommand, BookDetailDto>
{
    private readonly IApplicationDbContext _context;

    public CreateBookCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BookDetailDto> Handle(CreateBookCommand request, CancellationToken cancellationToken)
    {
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
                book => !book.IsDeleted && book.Isbn != null && book.Isbn == isbn,
                cancellationToken);
            if (isbnExists)
            {
                throw new InvalidOperationException("Ya existe un libro con ese ISBN.");
            }
        }

        Publisher? publisher = null;
        if (request.PublisherId.HasValue)
        {
            publisher = await _context.Publishers.FirstOrDefaultAsync(
                current => current.Id == request.PublisherId.Value && current.IsActive,
                cancellationToken);

            if (publisher is null)
            {
                throw new KeyNotFoundException("La editorial especificada no existe.");
            }
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

        var book = new Book
        {
            Title = title,
            Isbn = isbn,
            Description = description,
            PublisherId = request.PublisherId,
            PublicationYear = request.PublicationYear,
            Language = language,
            ImageUrl = imageUrl,
            Price = request.Price,
            IsActive = true,
        };

        var bookAuthors = authorIds.Select(authorId => new BookAuthor { AuthorId = authorId, Book = book }).ToList();
        var bookCategories = categoryIds.Select(categoryId => new BookCategory { CategoryId = categoryId, Book = book }).ToList();

        _context.Books.Add(book);
        _context.BookAuthors.AddRange(bookAuthors);
        _context.BookCategories.AddRange(bookCategories);

        await _context.SaveChangesAsync(cancellationToken);

        return BuildDetailDto(book, authorIds, categoryIds, authors, categories, publisher?.Name);
    }

    private static IReadOnlyCollection<Guid> GetDistinctIds(IReadOnlyCollection<Guid>? ids)
    {
        if (ids == null || ids.Count == 0)
        {
            return [];
        }

        return ids.Distinct().ToList();
    }

    private static BookDetailDto BuildDetailDto(
        Book book,
        IReadOnlyCollection<Guid> authorIds,
        IReadOnlyCollection<Guid> categoryIds,
        IReadOnlyCollection<Author> authors,
        IReadOnlyCollection<Category> categories,
        string? publisherName)
    {
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
            Authors = [..authorIds.Intersect(authors.Select(author => author.Id)).Select(id => authors.First(author => author.Id == id).FullName)],
            Categories = [..categoryIds.Intersect(categories.Select(category => category.Id)).Select(id => categories.First(category => category.Id == id).Name)],
            TotalStock = 0,
            StockByBranch = []
        };
    }
}
