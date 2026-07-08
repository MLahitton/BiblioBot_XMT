using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Book> Books { get; }
    DbSet<BookReview> BookReviews { get; }
    DbSet<Author> Authors { get; }
    DbSet<BookAuthor> BookAuthors { get; }
    DbSet<Category> Categories { get; }
    DbSet<BookCategory> BookCategories { get; }
    DbSet<Publisher> Publishers { get; }

    DbSet<Branch> Branches { get; }
    DbSet<InventoryStock> InventoryStocks { get; }
    DbSet<InventoryMovementType> InventoryMovementTypes { get; }
    DbSet<InventoryMovement> InventoryMovements { get; }

    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    DbSet<UserFavoriteBook> UserFavoriteBooks { get; }

    DbSet<SaleStatus> SaleStatuses { get; }
    DbSet<SaleOrigin> SaleOrigins { get; }
    DbSet<Sale> Sales { get; }
    DbSet<SaleDetail> SaleDetails { get; }
    DbSet<Invoice> Invoices { get; }

    DbSet<RequestType> RequestTypes { get; }
    DbSet<RequestStatus> RequestStatuses { get; }
    DbSet<InternalRequest> InternalRequests { get; }
    DbSet<InternalRequestItem> InternalRequestItems { get; }

    DbSet<ChatConversation> ChatConversations { get; }
    DbSet<ChatLog> ChatLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
