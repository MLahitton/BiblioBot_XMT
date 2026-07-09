using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class BiblioBotDbContext : DbContext, IApplicationDbContext
{
    public BiblioBotDbContext(DbContextOptions<BiblioBotDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<Permission> Permissions { get; set; } = null!;
    public DbSet<UserRole> UserRoles { get; set; } = null!;
    public DbSet<RolePermission> RolePermissions { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<BookReview> BookReviews { get; set; } = null!;
    public DbSet<Author> Authors { get; set; } = null!;
    public DbSet<BookAuthor> BookAuthors { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<BookCategory> BookCategories { get; set; } = null!;
    public DbSet<Publisher> Publishers { get; set; } = null!;

    public DbSet<Branch> Branches { get; set; } = null!;
    public DbSet<InventoryStock> InventoryStocks { get; set; } = null!;
    public DbSet<InventoryMovementType> InventoryMovementTypes { get; set; } = null!;
    public DbSet<InventoryMovement> InventoryMovements { get; set; } = null!;

    public DbSet<Cart> Carts { get; set; } = null!;
    public DbSet<CartItem> CartItems { get; set; } = null!;
    public DbSet<UserFavoriteBook> UserFavoriteBooks { get; set; } = null!;

    public DbSet<SaleStatus> SaleStatuses { get; set; } = null!;
    public DbSet<SaleOrigin> SaleOrigins { get; set; } = null!;
    public DbSet<Sale> Sales { get; set; } = null!;
    public DbSet<SaleDetail> SaleDetails { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;

    public DbSet<RequestType> RequestTypes { get; set; } = null!;
    public DbSet<RequestStatus> RequestStatuses { get; set; } = null!;
    public DbSet<InternalRequest> InternalRequests { get; set; } = null!;
    public DbSet<InternalRequestItem> InternalRequestItems { get; set; } = null!;

    public DbSet<ChatConversation> ChatConversations { get; set; } = null!;
    public DbSet<ChatLog> ChatLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BiblioBotDbContext).Assembly);
    }
}
