using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ChatConversationConfiguration : IEntityTypeConfiguration<ChatConversation>
{
    public void Configure(EntityTypeBuilder<ChatConversation> builder)
    {
        builder.ToTable("chat_conversations");
        builder.HasKey(cc => cc.Id);

        builder.Property(cc => cc.Id).HasColumnName("id");
        builder.Property(cc => cc.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(120)
            .IsRequired();
        builder.Property(cc => cc.UserId).HasColumnName("user_id");
        builder.Property(cc => cc.CurrentState)
            .HasColumnName("current_state")
            .HasMaxLength(80);
        builder.Property(cc => cc.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(cc => cc.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasIndex(cc => cc.SessionId).HasDatabaseName("ix_chat_conversations_session_id");
        builder.HasIndex(cc => cc.UserId).HasDatabaseName("ix_chat_conversations_user_id");

        builder
            .HasOne(cc => cc.User)
            .WithMany(u => u.ChatConversations)
            .HasForeignKey(cc => cc.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
