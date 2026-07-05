using System.ComponentModel.DataAnnotations;

namespace Api.Contracts.Chat;

public sealed class SendChatMessageRequest
{
    [Required]
    [MaxLength(120)]
    public string SessionId { get; init; } = string.Empty;

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;
}

