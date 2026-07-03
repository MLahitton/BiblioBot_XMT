using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements", table =>
        {
            table.HasCheckConstraint("ck_inventory_movements_quantity_positive", "quantity > 0");
            table.HasCheckConstraint("ck_inventory_movements_previous_stock_non_negative", "previous_stock >= 0");
            table.HasCheckConstraint("ck_inventory_movements_new_stock_non_negative", "new_stock >= 0");
        });
        builder.HasKey(im => im.Id);

        builder.Property(im => im.Id).HasColumnName("id");
        builder.Property(im => im.BookId).HasColumnName("book_id");
        builder.Property(im => im.BranchId).HasColumnName("branch_id");
        builder.Property(im => im.MovementTypeId).HasColumnName("movement_type_id");
        builder.Property(im => im.Quantity).HasColumnName("quantity").IsRequired();
        builder.Property(im => im.PreviousStock).HasColumnName("previous_stock").IsRequired();
        builder.Property(im => im.NewStock).HasColumnName("new_stock").IsRequired();
        builder.Property(im => im.Reason)
            .HasColumnName("reason")
            .HasMaxLength(250);
        builder.Property(im => im.SaleId).HasColumnName("sale_id");
        builder.Property(im => im.ActorId).HasColumnName("actor_id");
        builder.Property(im => im.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasOne(im => im.Book)
            .WithMany(b => b.InventoryMovements)
            .HasForeignKey(im => im.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(im => im.Branch)
            .WithMany(b => b.InventoryMovements)
            .HasForeignKey(im => im.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(im => im.MovementType)
            .WithMany(mt => mt.InventoryMovements)
            .HasForeignKey(im => im.MovementTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(im => im.Sale)
            .WithMany(s => s.InventoryMovements)
            .HasForeignKey(im => im.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(im => im.Actor)
            .WithMany(u => u.InventoryMovements)
            .HasForeignKey(im => im.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
