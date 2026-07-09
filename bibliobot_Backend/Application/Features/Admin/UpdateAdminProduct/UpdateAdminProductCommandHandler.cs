using Application.Common.Interfaces;
using Application.Features.Admin.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Admin.UpdateAdminProduct;

public sealed class UpdateAdminProductCommandHandler : IRequestHandler<UpdateAdminProductCommand, AdminProductDto?>
{
    private readonly IApplicationDbContext _context;

    public UpdateAdminProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminProductDto?> Handle(UpdateAdminProductCommand request, CancellationToken cancellationToken)
    {
        AdminProductMapping.Validate(request);

        var book = await _context.Books
            .Include(current => current.Publisher)
            .Include(current => current.BookAuthors)
                .ThenInclude(author => author.Author)
            .Include(current => current.BookCategories)
                .ThenInclude(category => category.Category)
            .Include(current => current.InventoryStocks)
                .ThenInclude(stock => stock.Branch)
            .Include(current => current.SaleDetails)
            .Include(current => current.UserFavoriteBooks)
            .FirstOrDefaultAsync(current => current.Id == request.Id && !current.IsDeleted, cancellationToken);

        if (book is null)
        {
            return null;
        }

        var isbn = string.IsNullOrWhiteSpace(request.Isbn) ? null : request.Isbn.Trim();
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

        _context.BookAuthors.RemoveRange(book.BookAuthors);
        _context.BookCategories.RemoveRange(book.BookCategories);

        book.Title = request.Title.Trim();
        book.Isbn = isbn;
        book.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        book.Publisher = publisher;
        book.PublisherId = publisher?.Id;
        book.PublicationYear = request.PublicationYear;
        book.Language = string.IsNullOrWhiteSpace(request.Language) ? null : request.Language.Trim();
        book.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        book.Price = request.Price;
        book.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var author in authors)
        {
            _context.BookAuthors.Add(new BookAuthor { Book = book, Author = author });
        }

        foreach (var category in categories)
        {
            _context.BookCategories.Add(new BookCategory { Book = book, Category = category });
        }

        var stock = book.InventoryStocks.FirstOrDefault(current => current.BranchId == branch.Id);
        if (stock is null)
        {
            stock = new InventoryStock
            {
                Book = book,
                Branch = branch,
            };

            book.InventoryStocks.Add(stock);
        }

        stock.CurrentStock = request.CurrentStock;
        stock.MinStock = request.MinStock;
        stock.UpdatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return AdminProductMapping.ToDto(book);
    }
}
