using Domain.Common;

namespace Domain.Entities;

public class ChatConversation : AuditableEntity
{
    public string SessionId { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? CurrentState { get; set; }

    public ICollection<ChatLog> ChatLogs { get; set; } = [];
}
