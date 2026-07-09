using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SaleStatusConfiguration : IEntityTypeConfiguration<SaleStatus>
{
    public void Configure(EntityTypeBuilder<SaleStatus> builder)
    {
        builder.ToTable("sale_statuses");
        builder.HasKey(ss => ss.Id);

        builder.Property(ss => ss.Id).HasColumnName("id");
        builder.Property(ss => ss.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(ss => ss.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(ss => ss.Code)
            .HasDatabaseName("uq_sale_statuses_code")
            .IsUnique();
    }
}
