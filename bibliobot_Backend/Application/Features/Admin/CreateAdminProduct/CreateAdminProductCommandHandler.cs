using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.CreateAdminProduct;

public sealed class CreateAdminProductCommandHandler : IRequestHandler<CreateAdminProductCommand, AdminProductDto>
{
    private readonly IApplicationDbContext _context;

    public CreateAdminProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminProductDto> Handle(CreateAdminProductCommand request, CancellationToken cancellationToken)
    {
        AdminProductMapping.Validate(request);

        var isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : request.Isbn.Trim();
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

        var publisher = await AdminProductMapping.ResolvePublisherAsync(
            _context,
            request.PublisherName,
            cancellationToken);
        var authors = await AdminProductMapping.ResolveAuthorsAsync(
            _context,
            request.AuthorNames,
            cancellationToken);
        var categories = await AdminProductMapping.ResolveCategoriesAsync(
            _context,
            request.CategoryNames,
            cancellationToken);
        var branch = await AdminProductMapping.ResolveBranchAsync(
            _context,
            request.BranchId,
            cancellationToken);

        var book = new Book
        {
            Title = request.Title.Trim(),
            Isbn = isbn,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Publisher = publisher,
            PublicationYear = request.PublicationYear,
            Language = string.IsNullOrWhiteSpace(request.Language) ? null : request.Language.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Price = request.Price,
            IsActive = true,
        };

        foreach (var author in authors)
        {
            book.BookAuthors.Add(new BookAuthor { Book = book, Author = author });
        }

        foreach (var category in categories)
        {
            book.BookCategories.Add(new BookCategory { Book = book, Category = category });
        }

        book.InventoryStocks.Add(
            new InventoryStock
            {
                Book = book,
                Branch = branch,
                CurrentStock = request.CurrentStock,
                MinStock = request.MinStock,
                UpdatedAt = DateTimeOffset.UtcNow,
            });

        _context.Books.Add(book);
        await _context.SaveChangesAsync(cancellationToken);

        return AdminProductMapping.ToDto(book);
    }
}
