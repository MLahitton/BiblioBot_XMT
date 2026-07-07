using System.Collections.Generic;

namespace Application.Features.Chat.Common;

public sealed class ChatbotResponseDto
{
    public string Response { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public IReadOnlyCollection<ChatLinkDto> Links { get; init; } = [];
    public string UiAction { get; init; } = "NONE";
    public object? Context { get; init; }
}

