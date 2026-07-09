namespace Application.Features.Chat.Common;

public sealed class ChatLinkDto
{
    public string Label { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string? Type { get; init; }
}

