using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class BookReviewConfiguration : IEntityTypeConfiguration<BookReview>
{
    public void Configure(EntityTypeBuilder<BookReview> builder)
    {
        builder.ToTable("book_reviews", table =>
        {
            table.HasCheckConstraint("ck_book_reviews_rating_range", "rating >= 1 AND rating <= 5");
        });

        builder.HasKey(review => review.Id);

        builder.Property(review => review.Id).HasColumnName("id");
        builder.Property(review => review.BookId).HasColumnName("book_id");
        builder.Property(review => review.UserId).HasColumnName("user_id");
        builder.Property(review => review.Rating)
            .HasColumnName("rating")
            .IsRequired();
        builder.Property(review => review.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1000)
            .IsRequired();
        builder.Property(review => review.IsVerifiedPurchase)
            .HasColumnName("is_verified_purchase")
            .IsRequired();
        builder.Property(review => review.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(review => review.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(review => review.BookId)
            .HasDatabaseName("ix_book_reviews_book_id");

        builder.HasIndex(review => new { review.UserId, review.BookId })
            .HasDatabaseName("uq_book_reviews_user_id_book_id")
            .IsUnique();

        builder
            .HasOne(review => review.Book)
            .WithMany(book => book.BookReviews)
            .HasForeignKey(review => review.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(review => review.User)
            .WithMany(user => user.BookReviews)
            .HasForeignKey(review => review.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
