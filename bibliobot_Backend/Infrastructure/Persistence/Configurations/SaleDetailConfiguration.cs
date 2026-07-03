using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SaleDetailConfiguration : IEntityTypeConfiguration<SaleDetail>
{
    public void Configure(EntityTypeBuilder<SaleDetail> builder)
    {
        builder.ToTable("sale_details", table =>
        {
            table.HasCheckConstraint("ck_sale_details_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_sale_details_unit_price_non_negative", "unit_price >= 0");
            table.HasCheckConstraint("ck_sale_details_line_total_non_negative", "line_total >= 0");
        });
        builder.HasKey(sd => sd.Id);

        builder.Property(sd => sd.Id).HasColumnName("id");
        builder.Property(sd => sd.SaleId).HasColumnName("sale_id");
        builder.Property(sd => sd.BookId).HasColumnName("book_id");
        builder.Property(sd => sd.BookTitleSnapshot)
            .HasColumnName("book_title_snapshot")
            .HasMaxLength(250)
            .IsRequired();
        builder.Property(sd => sd.IsbnSnapshot)
            .HasColumnName("isbn_snapshot")
            .HasMaxLength(30);
        builder.Property(sd => sd.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(sd => sd.UnitPrice)
            .HasColumnName("unit_price")
            .HasPrecision(12, 2)
            .IsRequired();
        builder.Property(sd => sd.LineTotal)
            .HasColumnName("line_total")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.HasOne(sd => sd.Sale)
            .WithMany(s => s.SaleDetails)
            .HasForeignKey(sd => sd.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sd => sd.Book)
            .WithMany(b => b.SaleDetails)
            .HasForeignKey(sd => sd.BookId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
