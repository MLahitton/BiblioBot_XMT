using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class UserFavoriteBookConfiguration : IEntityTypeConfiguration<UserFavoriteBook>
{
    public void Configure(EntityTypeBuilder<UserFavoriteBook> builder)
    {
        builder.ToTable("user_favorite_books");
        builder.HasKey(ufb => ufb.Id);

        builder.Property(ufb => ufb.Id).HasColumnName("id");
        builder.Property(ufb => ufb.UserId).HasColumnName("user_id");
        builder.Property(ufb => ufb.BookId).HasColumnName("book_id");
        builder.Property(ufb => ufb.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(ufb => ufb.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder
            .HasOne(ufb => ufb.User)
            .WithMany(u => u.UserFavoriteBooks)
            .HasForeignKey(ufb => ufb.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ufb => ufb.Book)
            .WithMany(b => b.UserFavoriteBooks)
            .HasForeignKey(ufb => ufb.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ufb => new { ufb.UserId, ufb.BookId })
            .HasDatabaseName("uq_user_favorite_books_user_id_book_id")
            .IsUnique();
    }
}
