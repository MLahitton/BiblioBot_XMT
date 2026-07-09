using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", table =>
        {
            table.HasCheckConstraint(
                "ck_users_soft_delete_state",
                "(is_deleted = false AND deleted_at IS NULL) OR (is_deleted = true AND deleted_at IS NOT NULL)");
        });
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(180)
            .IsRequired();
        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();
        builder.Property(u => u.Phone)
            .HasColumnName("phone")
            .HasMaxLength(40);
        builder.Property(u => u.DocumentNumber)
            .HasColumnName("document_number")
            .HasMaxLength(50);
        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
        builder.Property(u => u.IsDeleted)
            .HasColumnName("is_deleted")
            .IsRequired();
        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(u => u.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(u => u.Email)
            .HasDatabaseName("uq_users_email")
            .IsUnique();
        builder.HasIndex(u => u.DocumentNumber)
            .HasDatabaseName("ix_users_document_number");

    }
}
