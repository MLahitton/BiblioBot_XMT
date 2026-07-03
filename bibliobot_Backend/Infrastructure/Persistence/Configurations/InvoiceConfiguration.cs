using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", table =>
        {
            table.HasCheckConstraint("ck_invoices_subtotal_non_negative", "subtotal >= 0");
            table.HasCheckConstraint("ck_invoices_tax_non_negative", "tax_total >= 0");
            table.HasCheckConstraint("ck_invoices_total_non_negative", "total >= 0");
        });
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.SaleId).HasColumnName("sale_id");
        builder.Property(i => i.InvoiceNumber)
            .HasColumnName("invoice_number")
            .HasMaxLength(80)
            .IsRequired();
        builder.Property(i => i.CustomerId).HasColumnName("customer_id");
        builder.Property(i => i.Subtotal)
            .HasColumnName("subtotal")
            .HasPrecision(12, 2)
            .IsRequired();
        builder.Property(i => i.TaxTotal)
            .HasColumnName("tax_total")
            .HasPrecision(12, 2)
            .IsRequired();
        builder.Property(i => i.Total)
            .HasColumnName("total")
            .HasPrecision(12, 2)
            .IsRequired();
        builder.Property(i => i.IssuedAt)
            .HasColumnName("issued_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(i => i.IsCancelled)
            .HasColumnName("is_cancelled")
            .IsRequired();
        builder.Property(i => i.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(i => i.Sale)
            .WithOne(s => s.Invoice)
            .HasForeignKey<Invoice>(i => i.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.SaleId)
            .HasDatabaseName("uq_invoices_sale_id")
            .IsUnique();

        builder.HasIndex(i => i.InvoiceNumber)
            .HasDatabaseName("uq_invoices_invoice_number")
            .IsUnique();

    }
}
