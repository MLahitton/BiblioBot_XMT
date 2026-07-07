using Application.Features.Chat.Common;
using MediatR;
using System.Collections.Generic;

namespace Application.Features.Chat.SendChatMessage;

public sealed class SendChatMessageCommand : IRequest<ChatMessageResponseDto>
{
    public string SessionId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public IReadOnlyCollection<string> RolesFromClaims { get; init; } = [];
    public IReadOnlyCollection<string> PermissionsFromClaims { get; init; } = [];
}

