using System;

namespace Application.Features.Chat.Common;

public sealed class ChatbotRequestDto
{
    public string SessionId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public Guid? UserId { get; init; }
    public string? UserEmail { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = [];
    public IReadOnlyCollection<string> Permissions { get; init; } = [];
    public string Source { get; init; } = "DOTNET_BACKEND";
    public DateTimeOffset SentAt { get; init; }
}
