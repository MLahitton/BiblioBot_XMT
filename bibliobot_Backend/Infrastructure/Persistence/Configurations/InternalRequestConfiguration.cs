using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class InternalRequestConfiguration : IEntityTypeConfiguration<InternalRequest>
{
    public void Configure(EntityTypeBuilder<InternalRequest> builder)
    {
        builder.ToTable("internal_requests");
        builder.HasKey(ir => ir.Id);

        builder.Property(ir => ir.Id).HasColumnName("id");
        builder.Property(ir => ir.RequestTypeId).HasColumnName("request_type_id");
        builder.Property(ir => ir.StatusId).HasColumnName("status_id");
        builder.Property(ir => ir.ActorId).HasColumnName("actor_id");
        builder.Property(ir => ir.SourceBranchId).HasColumnName("source_branch_id");
        builder.Property(ir => ir.TargetBranchId).HasColumnName("target_branch_id");
        builder.Property(ir => ir.Description)
            .HasColumnName("description")
            .HasColumnType("text");
        builder.Property(ir => ir.Observations)
            .HasColumnName("observations")
            .HasColumnType("text");
        builder.Property(ir => ir.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(ir => ir.ReviewedAt)
            .HasColumnName("reviewed_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(ir => ir.ExecutedAt)
            .HasColumnName("executed_at")
            .HasColumnType("timestamp with time zone");

        builder.HasOne(ir => ir.RequestType)
            .WithMany(rt => rt.InternalRequests)
            .HasForeignKey(ir => ir.RequestTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ir => ir.Status)
            .WithMany(rs => rs.InternalRequests)
            .HasForeignKey(ir => ir.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ir => ir.Actor)
            .WithMany(u => u.InternalRequests)
            .HasForeignKey(ir => ir.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ir => ir.SourceBranch)
            .WithMany(b => b.SourceInternalRequests)
            .HasForeignKey(ir => ir.SourceBranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ir => ir.TargetBranch)
            .WithMany(b => b.TargetInternalRequests)
            .HasForeignKey(ir => ir.TargetBranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
