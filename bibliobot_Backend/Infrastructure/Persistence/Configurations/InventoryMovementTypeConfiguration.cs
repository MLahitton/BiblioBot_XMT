using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementTypeConfiguration : IEntityTypeConfiguration<InventoryMovementType>
{
    public void Configure(EntityTypeBuilder<InventoryMovementType> builder)
    {
        builder.ToTable("inventory_movement_types");
        builder.HasKey(imt => imt.Id);

        builder.Property(imt => imt.Id).HasColumnName("id");
        builder.Property(imt => imt.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(imt => imt.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(imt => imt.Code)
            .HasDatabaseName("uq_inventory_movement_types_code")
            .IsUnique();
    }
}
