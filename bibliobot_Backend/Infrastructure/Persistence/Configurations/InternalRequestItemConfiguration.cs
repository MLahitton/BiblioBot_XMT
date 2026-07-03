using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class InternalRequestItemConfiguration : IEntityTypeConfiguration<InternalRequestItem>
{
    public void Configure(EntityTypeBuilder<InternalRequestItem> builder)
    {
        builder.ToTable("internal_request_items", table =>
        {
            table.HasCheckConstraint("ck_internal_request_items_quantity_positive", "quantity > 0");
        });
        builder.HasKey(iri => iri.Id);

        builder.Property(iri => iri.Id).HasColumnName("id");
        builder.Property(iri => iri.InternalRequestId).HasColumnName("internal_request_id");
        builder.Property(iri => iri.BookId).HasColumnName("book_id");
        builder.Property(iri => iri.RequestedTitle)
            .HasColumnName("requested_title")
            .HasMaxLength(250);
        builder.Property(iri => iri.Quantity).HasColumnName("quantity");
        builder.Property(iri => iri.Observations)
            .HasColumnName("observations")
            .HasColumnType("text");

        builder.HasOne(iri => iri.InternalRequest)
            .WithMany(ir => ir.Items)
            .HasForeignKey(iri => iri.InternalRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(iri => iri.Book)
            .WithMany(b => b.InternalRequestItems)
            .HasForeignKey(iri => iri.BookId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
