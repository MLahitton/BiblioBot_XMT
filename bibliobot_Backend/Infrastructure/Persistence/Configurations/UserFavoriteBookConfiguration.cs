using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class UserFavoriteBookConfiguration : IEntityTypeConfiguration<UserFavoriteBook>
{
    public void Configure(EntityTypeBuilder<UserFavoriteBook> builder)
    {
        builder.ToTable("user_favorite_books");
        builder.HasKey(favorite => favorite.Id);

        builder.Property(favorite => favorite.Id).HasColumnName("id");
        builder.Property(favorite => favorite.UserId).HasColumnName("user_id");
        builder.Property(favorite => favorite.BookId).HasColumnName("book_id");
        builder.Property(favorite => favorite.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(favorite => favorite.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(favorite => new { favorite.UserId, favorite.BookId })
            .HasDatabaseName("uq_user_favorite_books_user_id_book_id")
            .IsUnique();

        builder
            .HasOne(favorite => favorite.User)
            .WithMany(user => user.UserFavoriteBooks)
            .HasForeignKey(favorite => favorite.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(favorite => favorite.Book)
            .WithMany(book => book.UserFavoriteBooks)
            .HasForeignKey(favorite => favorite.BookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
