using Application.Common.Interfaces;
using Application.Features.Cart.AddOrUpdateCartItem;
using Application.Features.Chat.Common;
using Application.Features.Sales.Common;
using Application.Features.Sales.ConfirmSale;
using Application.Features.Sales.CreateSale;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Application.Features.Chat.SendChatMessage;

public sealed class SendChatMessageCommandHandler : IRequestHandler<SendChatMessageCommand, ChatMessageResponseDto>
{
    private static readonly string[] GuestRoles = ["GUEST"];
    private static readonly string[] GuestPermissions =
    [
        "chat.message",
        "books.read",
        "books.search"
    ];

    private static readonly string[] AllowedUiActions =
    [
        "NAVIGATE_TO_CATALOG",
        "NAVIGATE_TO_PRODUCT",
        "OPEN_CART",
        "SHOW_INVOICE",
        "APPLY_FILTERS",
        "NONE"
    ];
    private static readonly ConcurrentDictionary<string, Guid> ProcessedCheckoutActions =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, Guid> ProcessedSaleConfirmationActions =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IChatbotClient _chatbotClient;
    private readonly ISender _sender;
    private readonly IConfiguration _configuration;

    public SendChatMessageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IChatbotClient chatbotClient,
        ISender sender,
        IConfiguration configuration)
    {
        _context = context;
        _currentUserService = currentUserService;
        _chatbotClient = chatbotClient;
        _sender = sender;
        _configuration = configuration;
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

        IReadOnlyCollection<string> roles = [];
        IReadOnlyCollection<string> permissions = [];
        Guid? actorId = null;
        Domain.Entities.User? user = null;

        if (!request.IsGuest)
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
            {
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            }

            actorId = _currentUserService.UserId.Value;
            user = await _context.Users.FirstOrDefaultAsync(
                existingUser => existingUser.Id == actorId,
                cancellationToken);

            if (user is null || !user.IsActive || user.IsDeleted)
            {
                throw new UnauthorizedAccessException("Usuario no autenticado.");
            }

            roles = await ResolveRolesAsync(request, actorId.Value, cancellationToken);
            permissions = await ResolvePermissionsAsync(request, actorId.Value, roles, cancellationToken);
        }
        else
        {
            if (request.UserId.HasValue && request.UserId.Value == Guid.Empty)
            {
                actorId = null;
            }
            else
            {
                actorId = request.UserId;
            }

            roles = request.RolesFromClaims.Count > 0
                ? request.RolesFromClaims.Select(role => role.Trim()).Distinct().ToArray()
                : GuestRoles;

            permissions = request.PermissionsFromClaims.Count > 0
                ? request.PermissionsFromClaims.Select(permission => permission.Trim()).Distinct().ToArray()
                : GuestPermissions;
        }

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
            UserEmail = request.IsGuest
                ? request.UserEmail
                : (_currentUserService.Email ?? user?.Email),
            Roles = roles,
            Permissions = permissions,
            Source = "DOTNET_BACKEND",
            SentAt = now,
            PageContext = request.PageContext,
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

        var cartMutationResponse = await TryApplyConfirmedCartMutationAsync(
            request,
            responseFromFastApi,
            actorId,
            permissions,
            cancellationToken);

        if (cartMutationResponse is not null)
        {
            return cartMutationResponse;
        }

        var saleMutationResponse = await TryApplyConfirmedCheckoutMutationAsync(
            request,
            responseFromFastApi,
            actorId,
            permissions,
            cancellationToken);

        if (saleMutationResponse is not null)
        {
            return saleMutationResponse;
        }

        var saleConfirmationResponse = await TryApplyConfirmedSaleConfirmationMutationAsync(
            request,
            responseFromFastApi,
            actorId,
            permissions,
            cancellationToken);

        if (saleConfirmationResponse is not null)
        {
            return saleConfirmationResponse;
        }

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

    private async Task<ChatMessageResponseDto?> TryApplyConfirmedCartMutationAsync(
        SendChatMessageCommand request,
        ChatbotResponseDto responseFromFastApi,
        Guid? actorId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken)
    {
        if (!TryGetConfirmedPurchaseAction(responseFromFastApi, out var action))
        {
            return null;
        }

        if (!IsRealCartMutationEnabled())
        {
            return null;
        }

        if (request.IsGuest || actorId is null || !_currentUserService.IsAuthenticated)
        {
            return BuildControlledChatResponse(
                responseFromFastApi,
                action,
                "Para agregar libros al carrito necesitas iniciar sesion.",
                "FAILED",
                "PERMISSION_DENIED",
                cartUpdated: false);
        }

        if (!HasPermission(permissions, PermissionCodes.CartManage))
        {
            return BuildControlledChatResponse(
                responseFromFastApi,
                action,
                "No tienes permiso para agregar libros al carrito.",
                "FAILED",
                "PERMISSION_DENIED",
                cartUpdated: false);
        }

        try
        {
            var (cart, _) = await _sender.Send(
                new AddOrUpdateCartItemCommand
                {
                    SessionId = request.SessionId,
                    BookId = action.BookId,
                    Quantity = action.Quantity,
                    BranchId = action.BranchId,
                },
                cancellationToken);

            var context = BuildUpdatedCartContext(responseFromFastApi, action, cart.TotalItems, cart.Subtotal);

            return new ChatMessageResponseDto
            {
                Response = $"Listo, agregue {action.Quantity} unidad(es) de {action.BookTitle ?? "este libro"} a tu carrito.",
                State = "DONE",
                UiAction = "OPEN_CART",
                Links =
                [
                    new ChatLinkDto
                    {
                        Label = "Ver carrito",
                        Url = "/cart",
                        Type = "CART",
                    }
                ],
                Context = context,
            };
        }
        catch (UnauthorizedAccessException)
        {
            return BuildControlledChatResponse(
                responseFromFastApi,
                action,
                "No tienes permiso para actualizar este carrito.",
                "FAILED",
                "PERMISSION_DENIED",
                cartUpdated: false);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            return BuildControlledChatResponse(
                responseFromFastApi,
                action,
                ex.Message,
                "FAILED",
                "CART_UPDATE_FAILED",
                cartUpdated: false);
        }
    }

    private async Task<ChatMessageResponseDto?> TryApplyConfirmedSaleConfirmationMutationAsync(
        SendChatMessageCommand request,
        ChatbotResponseDto responseFromFastApi,
        Guid? actorId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken)
    {
        if (!TryGetConfirmedSaleConfirmationAction(responseFromFastApi, out var action))
        {
            return null;
        }

        if (!IsRealSaleConfirmationMutationEnabled())
        {
            return null;
        }

        if (request.IsGuest || actorId is null || !_currentUserService.IsAuthenticated)
        {
            return BuildControlledSaleConfirmationResponse(
                responseFromFastApi,
                action,
                "Para confirmar una venta necesitas iniciar sesion.",
                "FAILED",
                "PERMISSION_DENIED",
                saleConfirmed: false);
        }

        if (!HasPermission(permissions, "sales.confirm"))
        {
            return BuildControlledSaleConfirmationResponse(
                responseFromFastApi,
                action,
                "No tienes permiso para confirmar ventas.",
                "FAILED",
                "PERMISSION_DENIED",
                saleConfirmed: false);
        }

        if (string.IsNullOrWhiteSpace(action.ActionRef))
        {
            return BuildControlledSaleConfirmationResponse(
                responseFromFastApi,
                action,
                "No pude identificar la accion confirmada. Vuelve a iniciar la confirmacion de venta.",
                "FAILED",
                "CONFIRM_SALE_ACTION_REF_REQUIRED",
                saleConfirmed: false);
        }

        if (ProcessedSaleConfirmationActions.TryGetValue(action.ActionRef, out var processedSaleId) && processedSaleId != Guid.Empty)
        {
            return BuildControlledSaleConfirmationResponse(
                responseFromFastApi,
                action with { SaleId = processedSaleId },
                "Esta accion ya habia confirmado la venta. No volvi a descontar inventario ni a crear factura duplicada.",
                "DONE",
                "SALE_CONFIRMATION_ALREADY_PROCESSED",
                saleConfirmed: true,
                isIdempotent: true);
        }

        if (!ProcessedSaleConfirmationActions.TryAdd(action.ActionRef, Guid.Empty))
        {
            return BuildControlledSaleConfirmationResponse(
                responseFromFastApi,
                action,
                "Esta confirmacion ya se esta procesando. No volvi a descontar inventario.",
                "DONE",
                "SALE_CONFIRMATION_ALREADY_PROCESSING",
                saleConfirmed: false,
                isIdempotent: true);
        }

        try
        {
            var saleId = await ResolveSaleIdForConfirmationAsync(action.SaleId, actorId.Value, cancellationToken);
            if (saleId is null)
            {
                ProcessedSaleConfirmationActions.TryRemove(action.ActionRef, out _);
                return BuildControlledSaleConfirmationResponse(
                    responseFromFastApi,
                    action,
                    "No encontre una venta pendiente unica para confirmar. Indica el identificador de la venta.",
                    "NEEDS_CLARIFICATION",
                    "ASK_SALE_ID",
                    saleConfirmed: false);
            }

            var saleForValidation = await _context.Sales
                .Include(sale => sale.Status)
                .FirstOrDefaultAsync(sale => sale.Id == saleId.Value, cancellationToken);

            if (saleForValidation is null)
            {
                ProcessedSaleConfirmationActions.TryRemove(action.ActionRef, out _);
                return BuildControlledSaleConfirmationResponse(
                    responseFromFastApi,
                    action with { SaleId = saleId },
                    "No encontre la venta indicada.",
                    "FAILED",
                    "SALE_NOT_FOUND",
                    saleConfirmed: false);
            }

            if (saleForValidation.BranchId is null)
            {
                ProcessedSaleConfirmationActions.TryRemove(action.ActionRef, out _);
                return BuildControlledSaleConfirmationResponse(
                    responseFromFastApi,
                    action with { SaleId = saleId },
                    "Para confirmar la venta necesito que la venta ya tenga una sede asignada. No confirme la venta ni toque inventario.",
                    "NEEDS_CLARIFICATION",
                    "ASK_BRANCH",
                    saleConfirmed: false);
            }

            var sale = await _sender.Send(
                new ConfirmSaleCommand
                {
                    Id = saleId.Value,
                },
                cancellationToken);

            ProcessedSaleConfirmationActions[action.ActionRef] = sale.Id;

            return new ChatMessageResponseDto
            {
                Response = BuildConfirmedSaleResponse(sale),
                State = "DONE",
                UiAction = "SHOW_INVOICE",
                Links = BuildInvoiceLinks(sale),
                Context = BuildConfirmedSaleContext(responseFromFastApi, action with { SaleId = sale.Id }, sale),
            };
        }
        catch (UnauthorizedAccessException)
        {
            ProcessedSaleConfirmationActions.TryRemove(action.ActionRef, out _);
            return BuildControlledSaleConfirmationResponse(
                responseFromFastApi,
                action,
                "No tienes permiso para confirmar esta venta.",
                "FAILED",
                "PERMISSION_DENIED",
                saleConfirmed: false);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            ProcessedSaleConfirmationActions.TryRemove(action.ActionRef, out _);
            return BuildControlledSaleConfirmationResponse(
                responseFromFastApi,
                action,
                ex.Message,
                "FAILED",
                "SALE_CONFIRMATION_FAILED",
                saleConfirmed: false);
        }
    }

    private async Task<ChatMessageResponseDto?> TryApplyConfirmedCheckoutMutationAsync(
        SendChatMessageCommand request,
        ChatbotResponseDto responseFromFastApi,
        Guid? actorId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken)
    {
        if (!TryGetConfirmedCheckoutAction(responseFromFastApi, out var action))
        {
            return null;
        }

        if (!IsRealSaleMutationEnabled())
        {
            return null;
        }

        if (request.IsGuest || actorId is null || !_currentUserService.IsAuthenticated)
        {
            return BuildControlledCheckoutResponse(
                responseFromFastApi,
                action,
                "Para finalizar el carrito necesitas iniciar sesion.",
                "FAILED",
                "PERMISSION_DENIED",
                saleCreated: false);
        }

        if (!HasPermission(permissions, PermissionCodes.SalesCreate))
        {
            return BuildControlledCheckoutResponse(
                responseFromFastApi,
                action,
                "No tienes permiso para crear ventas desde el carrito.",
                "FAILED",
                "PERMISSION_DENIED",
                saleCreated: false);
        }

        if (string.IsNullOrWhiteSpace(action.ActionRef))
        {
            return BuildControlledCheckoutResponse(
                responseFromFastApi,
                action,
                "No pude identificar la accion confirmada. Vuelve a iniciar el cierre del carrito.",
                "FAILED",
                "CHECKOUT_ACTION_REF_REQUIRED",
                saleCreated: false);
        }

        if (ProcessedCheckoutActions.TryGetValue(action.ActionRef, out var processedSaleId) && processedSaleId != Guid.Empty)
        {
            return BuildControlledCheckoutResponse(
                responseFromFastApi,
                action,
                "Esta accion ya habia creado una venta pendiente. No cree una venta duplicada.",
                "DONE",
                "SALE_ALREADY_CREATED",
                saleCreated: true,
                saleId: processedSaleId,
                isIdempotent: true);
        }

        if (!ProcessedCheckoutActions.TryAdd(action.ActionRef, Guid.Empty))
        {
            return BuildControlledCheckoutResponse(
                responseFromFastApi,
                action,
                "Esta accion ya se esta procesando. No cree una venta duplicada.",
                "DONE",
                "SALE_ALREADY_PROCESSING",
                saleCreated: false,
                isIdempotent: true);
        }

        try
        {
            var sale = await _sender.Send(
                new CreateSaleCommand
                {
                    SessionId = request.SessionId,
                    BranchId = action.BranchId,
                    OriginCode = SaleOriginCodes.Chatbot,
                },
                cancellationToken);

            ProcessedCheckoutActions[action.ActionRef] = sale.Id;

            return new ChatMessageResponseDto
            {
                Response = "Listo, cree una venta pendiente con los productos de tu carrito. Aun no se ha confirmado, no se genero factura y no se desconto inventario.",
                State = "DONE",
                UiAction = "OPEN_CART",
                Links =
                [
                    new ChatLinkDto
                    {
                        Label = "Ver carrito",
                        Url = "/cart",
                        Type = "CART",
                    }
                ],
                Context = BuildPendingSaleContext(responseFromFastApi, action, sale, isIdempotent: false),
            };
        }
        catch (UnauthorizedAccessException)
        {
            ProcessedCheckoutActions.TryRemove(action.ActionRef, out _);
            return BuildControlledCheckoutResponse(
                responseFromFastApi,
                action,
                "No tienes permiso para crear esta venta.",
                "FAILED",
                "PERMISSION_DENIED",
                saleCreated: false);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            ProcessedCheckoutActions.TryRemove(action.ActionRef, out _);
            return BuildControlledCheckoutResponse(
                responseFromFastApi,
                action,
                ex.Message,
                "FAILED",
                "CHECKOUT_CART_FAILED",
                saleCreated: false);
        }
    }

    private bool IsRealCartMutationEnabled()
    {
        var envValue = _configuration["BIBLIOBOT_ALLOW_REAL_CART_MUTATIONS"];
        if (bool.TryParse(envValue, out var envEnabled))
        {
            return envEnabled;
        }

        return _configuration.GetValue<bool>("ChatbotMutations:AllowRealCartMutations");
    }

    private bool IsRealSaleMutationEnabled()
    {
        var envValue = _configuration["BIBLIOBOT_ALLOW_REAL_SALE_MUTATIONS"];
        if (bool.TryParse(envValue, out var envEnabled))
        {
            return envEnabled;
        }

        return _configuration.GetValue<bool>("ChatbotMutations:AllowRealSaleMutations");
    }

    private bool IsRealSaleConfirmationMutationEnabled()
    {
        var envValue = _configuration["BIBLIOBOT_ALLOW_REAL_SALE_CONFIRMATION_MUTATIONS"];
        if (bool.TryParse(envValue, out var envEnabled))
        {
            return envEnabled;
        }

        return _configuration.GetValue<bool>("ChatbotMutations:AllowRealSaleConfirmationMutations");
    }

    private static bool HasPermission(IReadOnlyCollection<string> permissions, string permission)
    {
        return permissions.Any(value => string.Equals(value, permission, StringComparison.OrdinalIgnoreCase));
    }

    private static ChatMessageResponseDto BuildControlledChatResponse(
        ChatbotResponseDto responseFromFastApi,
        ConfirmedPurchaseAction action,
        string response,
        string state,
        string nextAction,
        bool cartUpdated)
    {
        var context = ToJsonObject(responseFromFastApi.Context);
        var metadata = GetOrCreateObject(context, "metadata");
        metadata["cartUpdated"] = cartUpdated;
        metadata["bookId"] = action.BookId.ToString();
        metadata["bookTitle"] = action.BookTitle;
        metadata["quantity"] = action.Quantity;
        metadata["actionRef"] = action.ActionRef;

        context["intent"] = "purchase_intent";
        context["requiresConfirmation"] = false;
        context["nextAction"] = nextAction;
        context["metadata"] = metadata;

        return new ChatMessageResponseDto
        {
            Response = response,
            State = state,
            UiAction = "NONE",
            Links = [],
            Context = context,
        };
    }

    private static ChatMessageResponseDto BuildControlledCheckoutResponse(
        ChatbotResponseDto responseFromFastApi,
        ConfirmedCheckoutAction action,
        string response,
        string state,
        string nextAction,
        bool saleCreated,
        Guid? saleId = null,
        bool isIdempotent = false)
    {
        var context = ToJsonObject(responseFromFastApi.Context);
        var metadata = GetOrCreateObject(context, "metadata");
        metadata["saleCreated"] = saleCreated;
        metadata["saleId"] = saleId?.ToString();
        metadata["actionRef"] = action.ActionRef;
        metadata["originCode"] = SaleOriginCodes.Chatbot;
        metadata["saleConfirmed"] = false;
        metadata["invoiceGenerated"] = false;
        metadata["inventoryDiscounted"] = false;
        metadata["cartCleared"] = false;
        metadata["isIdempotent"] = isIdempotent;
        metadata["realBackendMutationBlocked"] = false;

        context["intent"] = "checkout_cart";
        context["requiresConfirmation"] = false;
        context["nextAction"] = nextAction;
        context["metadata"] = metadata;

        return new ChatMessageResponseDto
        {
            Response = response,
            State = state,
            UiAction = "NONE",
            Links = [],
            Context = context,
        };
    }

    private async Task<Guid?> ResolveSaleIdForConfirmationAsync(
        Guid? requestedSaleId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        if (requestedSaleId.HasValue)
        {
            return requestedSaleId.Value;
        }

        var pendingSales = await _context.Sales
            .Include(sale => sale.Status)
            .Where(sale =>
                (sale.CustomerId == actorId || sale.ActorId == actorId)
                && sale.Status != null
                && (sale.Status.Code == SaleStatusCodes.PendingConfirmation
                    || sale.Status.Code == SaleStatusCodes.Created))
            .OrderByDescending(sale => sale.CreatedAt)
            .Take(2)
            .ToListAsync(cancellationToken);

        return pendingSales.Count == 1 ? pendingSales[0].Id : null;
    }

    private static ChatMessageResponseDto BuildControlledSaleConfirmationResponse(
        ChatbotResponseDto responseFromFastApi,
        ConfirmedSaleConfirmationAction action,
        string response,
        string state,
        string nextAction,
        bool saleConfirmed,
        bool isIdempotent = false)
    {
        var context = ToJsonObject(responseFromFastApi.Context);
        var metadata = GetOrCreateObject(context, "metadata");
        metadata["saleConfirmed"] = saleConfirmed;
        metadata["saleId"] = action.SaleId?.ToString();
        metadata["branchId"] = action.BranchId?.ToString();
        metadata["actionRef"] = action.ActionRef;
        metadata["invoiceGenerated"] = false;
        metadata["inventoryDiscounted"] = false;
        metadata["isIdempotent"] = isIdempotent;
        metadata["realBackendMutationBlocked"] = false;

        context["intent"] = "confirm_sale";
        context["requiresConfirmation"] = false;
        context["nextAction"] = nextAction;
        context["metadata"] = metadata;

        return new ChatMessageResponseDto
        {
            Response = response,
            State = state,
            UiAction = "NONE",
            Links = [],
            Context = context,
        };
    }

    private static string BuildConfirmedSaleResponse(SaleDto sale)
    {
        var invoiceNode = sale.Invoice is null ? null : JsonSerializer.SerializeToNode(sale.Invoice)?.AsObject();
        var invoiceNumber = GetString(invoiceNode, "InvoiceNumber") ?? GetString(invoiceNode, "invoiceNumber");
        var invoiceText = string.IsNullOrWhiteSpace(invoiceNumber)
            ? "La factura quedo asociada a la venta."
            : $"Factura generada: {invoiceNumber}.";

        if (sale.IsIdempotent)
        {
            return $"La venta {sale.Id} ya estaba confirmada. No volvi a descontar inventario ni a crear factura duplicada. {invoiceText}";
        }

        return $"Listo, confirme la venta {sale.Id}. Se desconto inventario y se genero la factura. {invoiceText}";
    }

    private static IReadOnlyCollection<ChatLinkDto> BuildInvoiceLinks(SaleDto sale)
    {
        var invoiceNode = sale.Invoice is null ? null : JsonSerializer.SerializeToNode(sale.Invoice)?.AsObject();
        var invoiceId = GetString(invoiceNode, "Id") ?? GetString(invoiceNode, "id");

        if (!string.IsNullOrWhiteSpace(invoiceId))
        {
            return
            [
                new ChatLinkDto
                {
                    Label = "Ver factura",
                    Url = $"/invoices/{invoiceId}",
                    Type = "INVOICE",
                }
            ];
        }

        return
        [
            new ChatLinkDto
            {
                Label = "Ver venta",
                Url = $"/sales/{sale.Id}",
                Type = "SALE",
            }
        ];
    }

    private static JsonObject BuildConfirmedSaleContext(
        ChatbotResponseDto responseFromFastApi,
        ConfirmedSaleConfirmationAction action,
        SaleDto sale)
    {
        var context = ToJsonObject(responseFromFastApi.Context);
        var metadata = GetOrCreateObject(context, "metadata");
        var invoiceNode = sale.Invoice is null ? null : JsonSerializer.SerializeToNode(sale.Invoice)?.AsObject();

        metadata["saleConfirmed"] = true;
        metadata["saleId"] = sale.Id.ToString();
        metadata["saleStatus"] = sale.StatusCode;
        metadata["saleStatusCode"] = sale.StatusCode;
        metadata["originCode"] = sale.OriginCode;
        metadata["actionRef"] = action.ActionRef;
        metadata["subtotal"] = sale.Subtotal;
        metadata["taxTotal"] = sale.TaxTotal;
        metadata["total"] = sale.Total;
        metadata["branchId"] = sale.BranchId?.ToString();
        metadata["confirmedAt"] = sale.ConfirmedAt?.ToString("O");
        metadata["invoiceGenerated"] = sale.Invoice is not null;
        metadata["inventoryDiscounted"] = !sale.IsIdempotent;
        metadata["isIdempotent"] = sale.IsIdempotent;
        metadata["realBackendMutationBlocked"] = false;

        if (invoiceNode is not null)
        {
            metadata["invoice"] = invoiceNode;
            metadata["invoiceId"] = GetString(invoiceNode, "Id") ?? GetString(invoiceNode, "id");
            metadata["invoiceNumber"] = GetString(invoiceNode, "InvoiceNumber") ?? GetString(invoiceNode, "invoiceNumber");
        }

        context["intent"] = "confirm_sale";
        context["requiresConfirmation"] = false;
        context["nextAction"] = sale.Invoice is not null ? "SHOW_INVOICE" : "SALE_CONFIRMED";
        context["metadata"] = metadata;

        return context;
    }

    private static JsonObject BuildUpdatedCartContext(
        ChatbotResponseDto responseFromFastApi,
        ConfirmedPurchaseAction action,
        int cartTotalItems,
        decimal cartSubtotal)
    {
        var context = ToJsonObject(responseFromFastApi.Context);
        var metadata = GetOrCreateObject(context, "metadata");

        metadata["cartUpdated"] = true;
        metadata["bookId"] = action.BookId.ToString();
        metadata["bookTitle"] = action.BookTitle;
        metadata["quantity"] = action.Quantity;
        metadata["actionRef"] = action.ActionRef;
        metadata["cartTotalItems"] = cartTotalItems;
        metadata["cartSubtotal"] = cartSubtotal;
        metadata["realBackendMutationBlocked"] = false;

        context["intent"] = "purchase_intent";
        context["requiresConfirmation"] = false;
        context["nextAction"] = "OPEN_CART";
        context["metadata"] = metadata;

        return context;
    }

    private static JsonObject BuildPendingSaleContext(
        ChatbotResponseDto responseFromFastApi,
        ConfirmedCheckoutAction action,
        SaleDto sale,
        bool isIdempotent)
    {
        var context = ToJsonObject(responseFromFastApi.Context);
        var metadata = GetOrCreateObject(context, "metadata");

        metadata["saleCreated"] = true;
        metadata["saleId"] = sale.Id.ToString();
        metadata["saleStatus"] = sale.StatusCode;
        metadata["saleStatusCode"] = sale.StatusCode;
        metadata["originCode"] = sale.OriginCode;
        metadata["actionRef"] = action.ActionRef;
        metadata["subtotal"] = sale.Subtotal;
        metadata["taxTotal"] = sale.TaxTotal;
        metadata["total"] = sale.Total;
        metadata["branchId"] = sale.BranchId?.ToString();
        metadata["saleConfirmed"] = false;
        metadata["invoiceGenerated"] = false;
        metadata["inventoryDiscounted"] = false;
        metadata["cartCleared"] = false;
        metadata["isIdempotent"] = isIdempotent;
        metadata["realBackendMutationBlocked"] = false;

        context["intent"] = "checkout_cart";
        context["requiresConfirmation"] = false;
        context["nextAction"] = "SALE_CREATED_PENDING_CONFIRMATION";
        context["metadata"] = metadata;

        return context;
    }

    private static bool TryGetConfirmedPurchaseAction(
        ChatbotResponseDto responseFromFastApi,
        out ConfirmedPurchaseAction action)
    {
        action = default;

        var context = ToJsonObject(responseFromFastApi.Context);
        var metadata = GetObject(context, "metadata");
        var confirmedAction = GetObject(metadata, "confirmedAction");

        if (confirmedAction is null)
        {
            return false;
        }

        var originalIntent = GetString(metadata, "originalIntent")
            ?? GetString(confirmedAction, "originalIntent")
            ?? GetString(context, "intent")
            ?? GetString(confirmedAction, "intent");

        if (!string.Equals(originalIntent, "purchase_intent", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nextAction = GetString(context, "nextAction");
        var status = GetString(confirmedAction, "status");
        var isConfirmed = string.Equals(nextAction, "CONFIRMATION_RECEIVED_MUTATION_BLOCKED", StringComparison.OrdinalIgnoreCase)
            || (status?.StartsWith("CONFIRMED", StringComparison.OrdinalIgnoreCase) ?? false);

        if (!isConfirmed)
        {
            return false;
        }

        var details = GetObject(confirmedAction, "details");
        var bookIdValue = GetString(confirmedAction, "bookId")
            ?? GetString(details, "bookId")
            ?? GetString(details, "book_id");

        if (!Guid.TryParse(bookIdValue, out var bookId))
        {
            return false;
        }

        var quantity = GetInt(confirmedAction, "quantity")
            ?? GetInt(details, "quantity");

        if (quantity is null or <= 0)
        {
            return false;
        }

        Guid? branchId = null;
        var branchIdValue = GetString(confirmedAction, "branchId")
            ?? GetString(details, "branchId")
            ?? GetString(details, "branch_id");

        if (Guid.TryParse(branchIdValue, out var parsedBranchId))
        {
            branchId = parsedBranchId;
        }

        action = new ConfirmedPurchaseAction(
            bookId,
            quantity.Value,
            GetString(confirmedAction, "bookTitle") ?? GetString(details, "bookTitle"),
            GetString(metadata, "actionRef") ?? GetString(confirmedAction, "actionRef"),
            branchId);

        return true;
    }

    private static bool TryGetConfirmedSaleConfirmationAction(
        ChatbotResponseDto responseFromFastApi,
        out ConfirmedSaleConfirmationAction action)
    {
        action = default;

        var context = ToJsonObject(responseFromFastApi.Context);
        var metadata = GetObject(context, "metadata");
        var confirmedAction = GetObject(metadata, "confirmedAction");

        if (confirmedAction is null)
        {
            return false;
        }

        var originalIntent = GetString(metadata, "originalIntent")
            ?? GetString(confirmedAction, "originalIntent")
            ?? GetString(context, "intent")
            ?? GetString(confirmedAction, "intent");

        if (!string.Equals(originalIntent, "confirm_sale", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(originalIntent, "sales_confirm", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nextAction = GetString(context, "nextAction");
        var status = GetString(confirmedAction, "status");
        var isConfirmed = string.Equals(nextAction, "CONFIRMATION_RECEIVED_MUTATION_BLOCKED", StringComparison.OrdinalIgnoreCase)
            || (status?.StartsWith("CONFIRMED", StringComparison.OrdinalIgnoreCase) ?? false);

        if (!isConfirmed)
        {
            return false;
        }

        var details = GetObject(confirmedAction, "details");
        Guid? saleId = null;
        var saleIdValue = GetString(metadata, "saleId")
            ?? GetString(confirmedAction, "saleId")
            ?? GetString(details, "saleId")
            ?? GetString(details, "sale_id");

        if (Guid.TryParse(saleIdValue, out var parsedSaleId))
        {
            saleId = parsedSaleId;
        }

        Guid? branchId = null;
        var branchIdValue = GetString(metadata, "branchId")
            ?? GetString(confirmedAction, "branchId")
            ?? GetString(details, "branchId")
            ?? GetString(details, "branch_id");

        if (Guid.TryParse(branchIdValue, out var parsedBranchId))
        {
            branchId = parsedBranchId;
        }

        action = new ConfirmedSaleConfirmationAction(
            saleId,
            GetString(metadata, "actionRef") ?? GetString(confirmedAction, "actionRef"),
            branchId);

        return true;
    }

    private static bool TryGetConfirmedCheckoutAction(
        ChatbotResponseDto responseFromFastApi,
        out ConfirmedCheckoutAction action)
    {
        action = default;

        var context = ToJsonObject(responseFromFastApi.Context);
        var metadata = GetObject(context, "metadata");
        var confirmedAction = GetObject(metadata, "confirmedAction");

        if (confirmedAction is null)
        {
            return false;
        }

        var originalIntent = GetString(metadata, "originalIntent")
            ?? GetString(confirmedAction, "originalIntent")
            ?? GetString(context, "intent")
            ?? GetString(confirmedAction, "intent");

        if (!string.Equals(originalIntent, "checkout_cart", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(originalIntent, "create_sale_from_cart", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var nextAction = GetString(context, "nextAction");
        var status = GetString(confirmedAction, "status");
        var isConfirmed = string.Equals(nextAction, "CONFIRMATION_RECEIVED_MUTATION_BLOCKED", StringComparison.OrdinalIgnoreCase)
            || (status?.StartsWith("CONFIRMED", StringComparison.OrdinalIgnoreCase) ?? false);

        if (!isConfirmed)
        {
            return false;
        }

        var details = GetObject(confirmedAction, "details");
        Guid? branchId = null;
        var branchIdValue = GetString(confirmedAction, "branchId")
            ?? GetString(details, "branchId")
            ?? GetString(details, "branch_id");

        if (Guid.TryParse(branchIdValue, out var parsedBranchId))
        {
            branchId = parsedBranchId;
        }

        action = new ConfirmedCheckoutAction(
            GetString(metadata, "actionRef") ?? GetString(confirmedAction, "actionRef"),
            branchId);

        return true;
    }

    private static JsonObject ToJsonObject(object? value)
    {
        if (value is null)
        {
            return [];
        }

        if (value is JsonObject jsonObject)
        {
            return (JsonObject)jsonObject.DeepClone();
        }

        if (value is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            return JsonNode.Parse(jsonElement.GetRawText())?.AsObject() ?? [];
        }

        return JsonSerializer.SerializeToNode(value)?.AsObject() ?? [];
    }

    private static JsonObject? GetObject(JsonObject? source, string name)
    {
        if (source is null || !source.TryGetPropertyValue(name, out var node) || node is null)
        {
            return null;
        }

        return node as JsonObject;
    }

    private static JsonObject GetOrCreateObject(JsonObject source, string name)
    {
        if (source.TryGetPropertyValue(name, out var node) && node is JsonObject existing)
        {
            return existing;
        }

        var created = new JsonObject();
        source[name] = created;
        return created;
    }

    private static string? GetString(JsonObject? source, string name)
    {
        if (source is null || !source.TryGetPropertyValue(name, out var node) || node is null)
        {
            return null;
        }

        if (node is JsonValue value && value.TryGetValue<string>(out var stringValue))
        {
            return stringValue;
        }

        return null;
    }

    private static int? GetInt(JsonObject? source, string name)
    {
        if (source is null || !source.TryGetPropertyValue(name, out var node) || node is null)
        {
            return null;
        }

        if (node is JsonValue value)
        {
            if (value.TryGetValue<int>(out var intValue))
            {
                return intValue;
            }

            if (value.TryGetValue<long>(out var longValue) && longValue <= int.MaxValue && longValue >= int.MinValue)
            {
                return (int)longValue;
            }

            if (value.TryGetValue<string>(out var stringValue) && int.TryParse(stringValue, out var parsedValue))
            {
                return parsedValue;
            }
        }

        return null;
    }

    private static ChatLog BuildAssistantErrorLog(
        ChatConversation conversation,
        Guid? actorId,
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

    private readonly record struct ConfirmedPurchaseAction(
        Guid BookId,
        int Quantity,
        string? BookTitle,
        string? ActionRef,
        Guid? BranchId);

    private readonly record struct ConfirmedCheckoutAction(
        string? ActionRef,
        Guid? BranchId);

    private readonly record struct ConfirmedSaleConfirmationAction(
        Guid? SaleId,
        string? ActionRef,
        Guid? BranchId);
}
