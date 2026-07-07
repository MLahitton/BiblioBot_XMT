using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items", table =>
        {
            table.HasCheckConstraint("ck_cart_items_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_cart_items_unit_price_non_negative", "unit_price >= 0");
        });
        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Id).HasColumnName("id");
        builder.Property(ci => ci.CartId).HasColumnName("cart_id");
        builder.Property(ci => ci.BookId).HasColumnName("book_id");
        builder.Property(ci => ci.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(ci => ci.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(12, 2)
            .IsRequired();
        builder.Property(ci => ci.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(ci => ci.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(ci => new { ci.CartId, ci.BookId })
            .HasDatabaseName("uq_cart_items_cart_book")
            .IsUnique();

        builder
            .HasOne(ci => ci.Cart)
            .WithMany(c => c.CartItems)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(ci => ci.Book)
            .WithMany(b => b.CartItems)
            .HasForeignKey(ci => ci.BookId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
