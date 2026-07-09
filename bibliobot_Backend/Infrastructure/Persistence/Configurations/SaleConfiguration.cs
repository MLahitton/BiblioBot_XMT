using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales", table =>
        {
            table.HasCheckConstraint("ck_sales_subtotal_non_negative", "subtotal >= 0");
            table.HasCheckConstraint("ck_sales_tax_non_negative", "tax_total >= 0");
            table.HasCheckConstraint("ck_sales_total_non_negative", "total >= 0");
        });
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.CustomerId).HasColumnName("customer_id");
        builder.Property(s => s.ActorId).HasColumnName("actor_id");
        builder.Property(s => s.BranchId).HasColumnName("branch_id");
        builder.Property(s => s.StatusId).HasColumnName("status_id");
        builder.Property(s => s.OriginId).HasColumnName("origin_id");
        builder.Property(s => s.Subtotal)
            .HasColumnName("subtotal")
            .HasPrecision(12, 2)
            .IsRequired();
        builder.Property(s => s.TaxTotal)
            .HasColumnName("tax_total")
            .HasPrecision(12, 2)
            .IsRequired();
        builder.Property(s => s.Total)
            .HasColumnName("total")
            .HasPrecision(12, 2)
            .IsRequired();
        builder.Property(s => s.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(s => s.RejectedReason)
            .HasColumnName("rejected_reason")
            .HasMaxLength(250);
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(s => s.Customer)
            .WithMany(u => u.CustomerSales)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Actor)
            .WithMany(u => u.ActorSales)
            .HasForeignKey(s => s.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Branch)
            .WithMany(b => b.SalesAsBranch)
            .HasForeignKey(s => s.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Status)
            .WithMany(ss => ss.Sales)
            .HasForeignKey(s => s.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Origin)
            .WithMany(so => so.Sales)
            .HasForeignKey(s => s.OriginId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.CustomerId).HasDatabaseName("ix_sales_customer_id");
        builder.HasIndex(s => s.ActorId).HasDatabaseName("ix_sales_actor_id");
        builder.HasIndex(s => s.BranchId).HasDatabaseName("ix_sales_branch_id");
        builder.HasIndex(s => s.StatusId).HasDatabaseName("ix_sales_status_id");
        builder.HasIndex(s => s.OriginId).HasDatabaseName("ix_sales_origin_id");
    }
}
