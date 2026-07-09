using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("books", table =>
        {
            table.HasCheckConstraint("ck_books_price_non_negative", "price >= 0");
            table.HasCheckConstraint(
                "ck_books_publication_year_positive",
                "publication_year > 0 OR publication_year IS NULL");
            table.HasCheckConstraint(
                "ck_books_soft_delete_state",
                "(is_deleted = false AND deleted_at IS NULL) OR (is_deleted = true AND deleted_at IS NOT NULL)");
        });
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).HasColumnName("id");
        builder.Property(b => b.Title)
            .HasColumnName("title")
            .HasMaxLength(250)
            .IsRequired();
        builder.Property(b => b.Isbn)
            .HasColumnName("isbn")
            .HasMaxLength(30);
        builder.Property(b => b.Description)
            .HasColumnName("description")
            .HasColumnType("text");
        builder.Property(b => b.PublisherId)
            .HasColumnName("publisher_id");
        builder.Property(b => b.PublicationYear)
            .HasColumnName("publication_year");
        builder.Property(b => b.Language)
            .HasColumnName("language")
            .HasMaxLength(50);
        builder.Property(b => b.ImageUrl)
            .HasColumnName("image_url")
            .HasColumnType("text");
        builder.Property(b => b.Price)
            .HasColumnName("price")
            .HasPrecision(12, 2)
            .IsRequired();
        builder.Property(b => b.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(b => b.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();
        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(b => b.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(b => b.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(b => b.Isbn)
            .HasDatabaseName("uq_books_isbn")
            .IsUnique()
            .HasFilter("isbn IS NOT NULL");

        builder.HasOne(b => b.Publisher)
            .WithMany(p => p.Books)
            .HasForeignKey(b => b.PublisherId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
