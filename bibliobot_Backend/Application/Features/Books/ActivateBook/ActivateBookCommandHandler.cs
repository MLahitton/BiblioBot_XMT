using Application.Common.Interfaces;
using Application.Features.Books.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Books.ActivateBook;

public sealed class ActivateBookCommandHandler : IRequestHandler<ActivateBookCommand, BookDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public ActivateBookCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BookDetailDto?> Handle(ActivateBookCommand request, CancellationToken cancellationToken)
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

        if (book.IsActive)
        {
            return MapBookDetailDto(book);
        }

        book.IsActive = true;
        book.UpdatedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return MapBookDetailDto(book);
    }

    private static BookDetailDto MapBookDetailDto(Book book)
    {
        return new BookDetailDto
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
            Authors = book.BookAuthors.Select(current => current.Author.FullName).Distinct().ToList(),
            Categories = book.BookCategories.Select(current => current.Category.Name).Distinct().ToList(),
            TotalStock = book.InventoryStocks.Sum(stock => stock.CurrentStock),
            StockByBranch = book.InventoryStocks.Select(stock => new BookBranchStockDto
            {
                BranchId = stock.BranchId,
                BranchName = stock.Branch.Name,
                CurrentStock = stock.CurrentStock,
            }).ToList(),
        };
    }
}

