using Domain.Common;

namespace Domain.Entities;

public class ChatLog : BaseEntity
{
    public Guid ConversationId { get; set; }
    public ChatConversation Conversation { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Direction { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Response { get; set; }
    public int? ProviderStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
