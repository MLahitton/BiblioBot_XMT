using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ChatLogConfiguration : IEntityTypeConfiguration<ChatLog>
{
    public void Configure(EntityTypeBuilder<ChatLog> builder)
    {
        builder.ToTable("chat_logs");
        builder.HasKey(cl => cl.Id);

        builder.Property(cl => cl.Id).HasColumnName("id");
        builder.Property(cl => cl.ConversationId).HasColumnName("conversation_id");
        builder.Property(cl => cl.UserId).HasColumnName("user_id");
        builder.Property(cl => cl.Direction)
            .HasColumnName("direction")
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(cl => cl.Message)
            .HasColumnName("message")
            .HasColumnType("text")
            .IsRequired();
        builder.Property(cl => cl.Response)
            .HasColumnName("response")
            .HasColumnType("text");
        builder.Property(cl => cl.ProviderStatusCode).HasColumnName("provider_status_code");
        builder.Property(cl => cl.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text");
        builder.Property(cl => cl.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasOne(cl => cl.Conversation)
            .WithMany(cc => cc.ChatLogs)
            .HasForeignKey(cl => cl.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(cl => cl.User)
            .WithMany(u => u.ChatLogs)
            .HasForeignKey(cl => cl.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
