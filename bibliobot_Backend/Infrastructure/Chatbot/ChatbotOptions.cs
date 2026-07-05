namespace Infrastructure.Chatbot;

public sealed class ChatbotOptions
{
    public string BaseUrl { get; init; } = "http://localhost:8000";
    public string MessagePath { get; init; } = "/api/chat/message";
    public int TimeoutSeconds { get; init; } = 30;
}

