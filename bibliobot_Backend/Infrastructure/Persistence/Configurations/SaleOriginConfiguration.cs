using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SaleOriginConfiguration : IEntityTypeConfiguration<SaleOrigin>
{
    public void Configure(EntityTypeBuilder<SaleOrigin> builder)
    {
        builder.ToTable("sale_origins");
        builder.HasKey(so => so.Id);

        builder.Property(so => so.Id).HasColumnName("id");
        builder.Property(so => so.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(so => so.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(so => so.Code)
            .HasDatabaseName("uq_sale_origins_code")
            .IsUnique();
    }
}
