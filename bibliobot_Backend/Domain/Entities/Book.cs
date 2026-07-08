using Domain.Common;

namespace Domain.Entities;

public class Book : SoftDeletableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Isbn { get; set; }
    public string? Description { get; set; }
    public Guid? PublisherId { get; set; }
    public Publisher? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string? Language { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];
    public ICollection<BookCategory> BookCategories { get; set; } = [];
    public ICollection<InventoryStock> InventoryStocks { get; set; } = [];
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
    public ICollection<CartItem> CartItems { get; set; } = [];
    public ICollection<SaleDetail> SaleDetails { get; set; } = [];
    public ICollection<InternalRequestItem> InternalRequestItems { get; set; } = [];
    public ICollection<UserFavoriteBook> UserFavoriteBooks { get; set; } = [];
    public ICollection<BookReview> BookReviews { get; set; } = [];
}
