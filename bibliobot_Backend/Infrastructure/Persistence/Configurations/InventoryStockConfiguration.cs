using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class InventoryStockConfiguration : IEntityTypeConfiguration<InventoryStock>
{
    public void Configure(EntityTypeBuilder<InventoryStock> builder)
    {
        builder.ToTable("inventory_stocks", table =>
        {
            table.HasCheckConstraint("ck_inventory_stocks_current_stock_non_negative", "current_stock >= 0");
            table.HasCheckConstraint("ck_inventory_stocks_min_stock_non_negative", "min_stock >= 0");
        });
        builder.HasKey(isb => isb.Id);

        builder.Property(isb => isb.Id).HasColumnName("id");
        builder.Property(isb => isb.BookId).HasColumnName("book_id");
        builder.Property(isb => isb.BranchId).HasColumnName("branch_id");
        builder.Property(isb => isb.CurrentStock).HasColumnName("current_stock").IsRequired();
        builder.Property(isb => isb.MinStock).HasColumnName("min_stock").IsRequired();
        builder.Property(isb => isb.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(isb => new { isb.BookId, isb.BranchId })
            .HasDatabaseName("uq_inventory_stocks_book_branch")
            .IsUnique();

        builder
            .HasOne(isb => isb.Book)
            .WithMany(b => b.InventoryStocks)
            .HasForeignKey(isb => isb.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(isb => isb.Branch)
            .WithMany(b => b.InventoryStocks)
            .HasForeignKey(isb => isb.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
