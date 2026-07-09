using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(c => c.UserId).HasColumnName("user_id");
        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(c => c.SessionId).HasDatabaseName("ix_carts_session_id");
        builder.HasIndex(c => c.UserId).HasDatabaseName("ix_carts_user_id");

        builder
            .HasOne(c => c.User)
            .WithMany(u => u.Carts)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
