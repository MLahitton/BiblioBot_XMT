using Application.Features.Chat.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IChatbotClient
{
    Task<ChatbotResponseDto> SendMessageAsync(
        ChatbotRequestDto request,
        CancellationToken cancellationToken);
}

