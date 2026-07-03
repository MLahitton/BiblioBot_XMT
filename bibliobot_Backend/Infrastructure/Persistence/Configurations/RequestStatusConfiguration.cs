using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class RequestStatusConfiguration : IEntityTypeConfiguration<RequestStatus>
{
    public void Configure(EntityTypeBuilder<RequestStatus> builder)
    {
        builder.ToTable("request_statuses");
        builder.HasKey(rs => rs.Id);

        builder.Property(rs => rs.Id).HasColumnName("id");
        builder.Property(rs => rs.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(rs => rs.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(rs => rs.Code)
            .HasDatabaseName("uq_request_statuses_code")
            .IsUnique();
    }
}
