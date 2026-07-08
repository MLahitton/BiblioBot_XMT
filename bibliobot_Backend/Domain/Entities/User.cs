using Domain.Common;

namespace Domain.Entities;

public class User : SoftDeletableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? DocumentNumber { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<Sale> CustomerSales { get; set; } = [];
    public ICollection<Sale> ActorSales { get; set; } = [];
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = [];
    public ICollection<InternalRequest> InternalRequests { get; set; } = [];
    public ICollection<Cart> Carts { get; set; } = [];
    public ICollection<ChatConversation> ChatConversations { get; set; } = [];
    public ICollection<ChatLog> ChatLogs { get; set; } = [];
    public ICollection<UserFavoriteBook> UserFavoriteBooks { get; set; } = [];
    public ICollection<BookReview> BookReviews { get; set; } = [];
}
