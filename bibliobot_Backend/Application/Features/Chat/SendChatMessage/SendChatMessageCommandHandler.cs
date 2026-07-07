using Application.Common.Interfaces;
using Application.Features.Chat.Common;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.Chat.SendChatMessage;

public sealed class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, ChatMessageResponseDto>
{
    private static readonly string[] AllowedUiActions =
    [
        "NAVIGATE_TO_CATALOG",
        "NAVIGATE_TO_PRODUCT",
        "OPEN_CART",
        "SHOW_INVOICE",
        "APPLY_FILTERS",
        "NONE"
    ];

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatbotClient _chatbotClient;

    public SendChatMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IChatbotClient chatbotClient)
    {
        _context = context;
        _currentUserService = currentUserService;
        _chatbotClient = chatbotClient;
    }

    public async Task<ChatMessageResponseDto> Handle(
        SendChatMessageCommand request,
        CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId?.Trim();
        var message = request.Message?.Trim();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("sessionId es obligatorio.");
        }

        if (sessionId.Length > 120)
        {
            throw new ArgumentException("sessionId no puede superar los 120 caracteres.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("message es obligatorio.");
        }

        if (message.Length > 4000)
        {
            throw new ArgumentException("message no puede superar los 4000 caracteres.");
        }

        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var actorId = _currentUserService.UserId.Value;
        var user = await _context.Users.FirstOrDefaultAsync(
            existingUser => existingUser.Id == actorId,
            cancellationToken);

        if (user is null || !user.IsActive || user.IsDeleted)
        {
            throw new UnauthorizedAccessException("Usuario no autenticado.");
        }

        var roles = await ResolveRolesAsync(request, actorId, cancellationToken);
        var permissions = await ResolvePermissionsAsync(request, actorId, roles, cancellationToken);

        var conversation = await _context.ChatConversations
            .FirstOrDefaultAsync(existing => existing.SessionId == sessionId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (conversation is null)
        {
            conversation = new ChatConversation
            {
                SessionId = sessionId,
                UserId = actorId,
                CurrentState = null
            };

            _context.ChatConversations.Add(conversation);
        }
        else if (conversation.UserId is null)
        {
            conversation.UserId = actorId;
        }

        _context.ChatLogs.Add(new ChatLog
        {
            Conversation = conversation,
            UserId = actorId,
            Direction = "USER",
            Message = message,
            CreatedAt = now,
        });

        await _context.SaveChangesAsync(cancellationToken);

        var requestPayload = new ChatbotRequestDto
        {
            SessionId = sessionId,
            Message = message,
            UserId = actorId,
            UserEmail = _currentUserService.Email ?? user.Email,
            Roles = roles,
            Permissions = permissions,
            Source = "DOTNET_BACKEND",
            SentAt = now,
        };

        ChatbotResponseDto responseFromFastApi;
        try
        {
            responseFromFastApi = await _chatbotClient.SendMessageAsync(requestPayload, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _context.ChatLogs.Add(BuildAssistantErrorLog(conversation, actorId, ex.Message, now));
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _context.ChatLogs.Add(BuildAssistantErrorLog(conversation, actorId, ex.Message, now));
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _context.ChatLogs.Add(BuildAssistantErrorLog(conversation, actorId, ex.Message, now));
            await _context.SaveChangesAsync(cancellationToken);
            throw;
        }

        conversation.CurrentState = responseFromFastApi.State;
        _context.ChatLogs.Add(new ChatLog
        {
            Conversation = conversation,
            UserId = actorId,
            Direction = "ASSISTANT",
            Message = responseFromFastApi.Response,
            Response = responseFromFastApi.Response,
            CreatedAt = now,
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new ChatMessageResponseDto
        {
            Response = responseFromFastApi.Response,
            State = responseFromFastApi.State,
            Links = responseFromFastApi.Links ?? [],
            UiAction = NormalizeUiAction(responseFromFastApi.UiAction),
            Context = responseFromFastApi.Context,
        };
    }

    private static string NormalizeUiAction(string? uiAction)
    {
        if (string.IsNullOrWhiteSpace(uiAction))
        {
            return "NONE";
        }

        var normalized = uiAction.Trim().ToUpperInvariant();
        return AllowedUiActions.Contains(normalized) ? normalized : "NONE";
    }

    private static ChatLog BuildAssistantErrorLog(
        ChatConversation conversation,
        Guid actorId,
        string errorMessage,
        DateTimeOffset createdAt)
    {
        return new ChatLog
        {
            Conversation = conversation,
            UserId = actorId,
            Direction = "ASSISTANT",
            Message = string.Empty,
            ErrorMessage = errorMessage,
            ProviderStatusCode = 0,
            CreatedAt = createdAt,
        };
    }

    private async Task<IReadOnlyCollection<string>> ResolveRolesAsync(
        SendChatMessageCommand request,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (request.RolesFromClaims.Count > 0)
        {
            return request.RolesFromClaims
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role.Trim())
                .Distinct()
                .ToArray();
        }

        return await _context.UserRoles
            .Where(userRole => userRole.UserId == actorId)
            .Join(_context.Roles, userRole => userRole.RoleId, role => role.Id, (_, role) => role.Code)
            .Where(roleCode => !string.IsNullOrWhiteSpace(roleCode))
            .Distinct()
            .OrderBy(roleCode => roleCode)
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<string>> ResolvePermissionsAsync(
        SendChatMessageCommand request,
        Guid actorId,
        IReadOnlyCollection<string> resolvedRoles,
        CancellationToken cancellationToken)
    {
        if (request.PermissionsFromClaims.Count > 0)
        {
            return request.PermissionsFromClaims
                .Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Select(permission => permission.Trim())
                .Distinct()
                .ToArray();
        }

        if (resolvedRoles.Count == 0)
        {
            return [];
        }

        var roleIds = await _context.UserRoles
            .Where(userRole => userRole.UserId == actorId)
            .Select(userRole => userRole.RoleId)
            .ToListAsync(cancellationToken);

        return await _context.RolePermissions
            .Where(rolePermission => roleIds.Contains(rolePermission.RoleId))
            .Join(_context.Permissions, rolePermission => rolePermission.PermissionId, permission => permission.Id, (_, permission) => permission.Code)
            .Where(permissionCode => !string.IsNullOrWhiteSpace(permissionCode))
            .Distinct()
            .OrderBy(permissionCode => permissionCode)
            .ToListAsync(cancellationToken);
    }
}
