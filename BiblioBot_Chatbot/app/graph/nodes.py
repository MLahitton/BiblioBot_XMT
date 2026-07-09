import re
import unicodedata
from collections.abc import Callable
from typing import Any

from app.schemas.chat_contract import ChatLink, ChatProcessRequest, ChatState, UiActionType
from app.services.auth_required_service import AuthRequiredService
from app.services.confirmation_service import ConfirmationService
from app.services.frontend_action_service import FrontendActionService
from app.services.llm_assistant_service import LlmAssistantService
from app.services.permission_service import PermissionService
from app.tools.bibliobot_tools import BiblioBotToolService
from app.tools.tool_context import ToolExecutionContext
from app.tools.tool_schemas import (
    AddOrUpdateCartItemInput,
    CheckStockInput,
    CreatePurchaseRequestInput,
    CreateTransferRequestInput,
    GetBookDetailInput,
    GetInvoiceInput,
    QuerySalesInput,
    RegisterInventoryEntryInput,
    SearchBooksInput,
)

from app.graph.state import ChatGraphState


SENSITIVE_INTENTS = {"purchase_intent", "inventory_entry", "transfer_request", "purchase_request"}
AUTH_REQUIRED_SERVICE = AuthRequiredService()
FRONTEND_ACTION_SERVICE = FrontendActionService()
ALLOWED_INTENTS = [
    "catalog_search",
    "book_detail",
    "stock_check",
    "purchase_intent",
    "invoice_query",
    "inventory_entry",
    "transfer_request",
    "purchase_request",
    "sales_query",
    "general_help",
    "unknown",
]
GENRE_WORDS = {
    "fantasia",
    "terror",
    "romance",
    "ciencia ficcion",
    "historia",
    "programacion",
    "software",
}


def normalize_input_node(state: ChatGraphState) -> ChatGraphState:
    request = state["request"]
    normalized_message = _normalize(request.message)
    return {
        **state,
        "session_id": request.sessionId,
        "user_id": str(request.userId) if request.userId else None,
        "user_email": request.userEmail,
        "roles": list(request.roles),
        "permissions": list(request.permissions),
        "source": request.source,
        "message": request.message,
        "normalized_message": normalized_message,
        "intent": "unknown",
        "state": ChatState.IDLE.value,
        "response": "",
        "ui_action": UiActionType.NONE.value,
        "links": [],
        "context": {},
        "metadata": _base_metadata(request, "unknown"),
        "requires_confirmation": False,
        "action_ref": None,
        "pending_action": None,
        "tool_result": None,
        "error": None,
        "is_terminal": False,
        "next_step": None,
    }


def make_base_validation_node(permission_service: PermissionService) -> Callable[[ChatGraphState], ChatGraphState]:
    def base_validation_node(state: ChatGraphState) -> ChatGraphState:
        permissions = state.get("permissions", [])
        if not state.get("session_id", "").strip():
            return _terminal_response(
                state,
                response="Necesito un sessionId valido para mantener la trazabilidad de la conversacion.",
                chat_state=ChatState.NEEDS_CLARIFICATION,
                intent="missing_session",
                next_action="REQUEST_VALID_SESSION",
            )
        if not permission_service.has_permission(permissions, "chat.message"):
            return _terminal_response(
                state,
                response="No puedo abrir el chat porque tu usuario no tiene el permiso necesario.",
                chat_state=ChatState.FAILED,
                intent="permission_denied",
                next_action="PERMISSION_DENIED",
            )
        if not state.get("roles"):
            return _terminal_response(
                state,
                response="No pude identificar tu sesion correctamente. Verifica tus datos e intenta nuevamente.",
                chat_state=ChatState.FAILED,
                intent="missing_roles",
                next_action="REQUEST_VALID_ROLE",
            )
        return state

    return base_validation_node


def make_confirmation_control_node(
    confirmation_service: ConfirmationService,
) -> Callable[[ChatGraphState], ChatGraphState]:
    def confirmation_control_node(state: ChatGraphState) -> ChatGraphState:
        message = state.get("message", "")
        if confirmation_service.is_explicit_cancellation(message):
            return _terminal_response(
                state,
                response="Listo, cancele la accion pendiente. No se realizo ningun cambio.",
                chat_state=ChatState.IDLE,
                intent="cancel_confirmation",
                next_action="WAITING_USER_MESSAGE",
            )
        if confirmation_service.is_explicit_confirmation(message):
            return _terminal_response(
                state,
                response="Aun no tengo una accion pendiente para confirmar. Dime primero que necesitas hacer.",
                chat_state=ChatState.NEEDS_CLARIFICATION,
                intent="confirmation_without_pending_action",
                next_action="ASK_ACTION_DETAILS",
            )
        return state

    return confirmation_control_node


def make_intent_detection_node(
    permission_service: PermissionService,
    llm_assistant_service: LlmAssistantService,
) -> Callable[[ChatGraphState], ChatGraphState]:
    def intent_detection_node(state: ChatGraphState) -> ChatGraphState:
        intent = _detect_intent(state.get("normalized_message", ""))
        if intent == "unknown":
            llm_intent = llm_assistant_service.suggest_intent(
                state.get("message", ""),
                _allowed_llm_intents(permission_service),
            )
            if llm_intent in ALLOWED_INTENTS:
                intent = llm_intent
        metadata = {**state.get("metadata", {}), "detectedIntent": intent}
        return {**state, "intent": intent, "metadata": metadata, "state": ChatState.INTENT_DETECTED.value}

    return intent_detection_node


def make_permission_check_node(permission_service: PermissionService) -> Callable[[ChatGraphState], ChatGraphState]:
    def permission_check_node(state: ChatGraphState) -> ChatGraphState:
        intent = state.get("intent", "unknown")
        permissions = state.get("permissions", [])
        if _is_guest_state(state) and AUTH_REQUIRED_SERVICE.requires_authenticated_user(intent):
            return _auth_required_response(state, intent)
        if not permission_service.can_access_intent(intent, permissions):
            required = permission_service.required_permissions_for_intent(intent)
            return _terminal_response(
                state,
                response=_permission_denied_message(intent),
                chat_state=ChatState.FAILED,
                intent=intent,
                next_action="PERMISSION_DENIED",
                metadata_extra={"requiredPermissions": required},
            )
        return state

    return permission_check_node


def make_tool_dispatch_node(tool_service: BiblioBotToolService) -> Callable[[ChatGraphState], ChatGraphState]:
    def tool_dispatch_node(state: ChatGraphState) -> ChatGraphState:
        intent = state.get("intent", "unknown")
        context = _tool_context(state)
        message = state.get("message", "")

        if intent == "catalog_search":
            query = _extract_catalog_query(message)
            result = tool_service.search_books(SearchBooksInput(query=query), context)
            return _catalog_result_state(state, query, result)

        if intent == "book_detail":
            book = _find_book_from_message(message, tool_service)
            if not book:
                return _asking_details(
                    state,
                    "Claro. Indica el nombre o identificador del libro y reviso el detalle.",
                    "ASK_BOOK_IDENTIFIER",
                )
            result = tool_service.get_book_detail(GetBookDetailInput(book_id=book["id"]), context)
            return _book_detail_result_state(state, result)

        if intent == "stock_check":
            book = _find_book_from_message(message, tool_service)
            if not book:
                return _asking_details(
                    state,
                    "Indica el libro y, si aplica, la sede para revisar disponibilidad.",
                    "ASK_BOOK_AND_BRANCH",
                )
            result = tool_service.check_stock(CheckStockInput(book_id=book["id"]), context)
            return _stock_result_state(state, result)

        if intent == "invoice_query":
            invoice_id = _extract_invoice_id(message)
            if not invoice_id:
                return _asking_details(
                    state,
                    "Indica el numero de factura o el identificador de venta que quieres consultar.",
                    "ASK_INVOICE_OR_SALE_ID",
                )
            result = tool_service.get_invoice(GetInvoiceInput(invoice_id=invoice_id), context)
            return _invoice_result_state(state, invoice_id, result)

        if intent == "sales_query":
            scope = "all" if "sales.read_all" in state.get("permissions", []) else "own"
            result = tool_service.query_sales(QuerySalesInput(scope=scope), context)
            return _sales_result_state(state, result)

        if intent == "purchase_intent":
            book = _find_book_from_message(message, tool_service)
            quantity = _extract_quantity(message)
            if not book or not quantity:
                return _asking_details(
                    state,
                    "Indica el libro y la cantidad. No preparare ninguna compra sin esos datos y sin tu confirmacion.",
                    "ASK_BOOK_AND_QUANTITY",
                    requires_confirmation=True,
                )
            result = tool_service.add_or_update_cart_item(
                AddOrUpdateCartItemInput(
                    session_id=state.get("session_id", ""),
                    book_id=book["id"],
                    quantity=quantity,
                ),
                context,
            )
            return _pending_confirmation_state(
                state,
                _enrich_purchase_pending_result(result, book, quantity),
                f"Preparar compra de {quantity} unidad(es) de {book['title']}",
                selected_book_id=book["id"],
                metadata_extra={"bookTitle": book["title"], "quantity": quantity},
            )

        if intent == "inventory_entry":
            details = _extract_sensitive_details(message, tool_service, require_branch=True)
            if not details:
                return _sensitive_details_state(state, "ASK_INVENTORY_ENTRY_DETAILS", intent)
            result = tool_service.register_inventory_entry(
                RegisterInventoryEntryInput(
                    book_id=details["book_id"],
                    branch_id=details["branch_id"],
                    quantity=details["quantity"],
                    reason=details.get("notes"),
                ),
                context,
            )
            return _pending_confirmation_state(state, result, "Preparar entrada de inventario simulada")

        if intent == "transfer_request":
            details = _extract_sensitive_details(message, tool_service, needs_two_branches=True)
            if not details:
                return _sensitive_details_state(state, "ASK_TRANSFER_DETAILS", intent)
            result = tool_service.create_transfer_request(
                CreateTransferRequestInput(
                    source_branch_id=details["source_branch_id"],
                    destination_branch_id=details["destination_branch_id"],
                    book_id=details["book_id"],
                    quantity=details["quantity"],
                    notes=details.get("notes"),
                ),
                context,
            )
            return _pending_confirmation_state(state, result, "Preparar solicitud de traslado simulada")

        if intent == "purchase_request":
            details = _extract_sensitive_details(message, tool_service, require_branch=True)
            if not details:
                return _sensitive_details_state(state, "ASK_PURCHASE_REQUEST_DETAILS", intent)
            result = tool_service.create_purchase_request(
                CreatePurchaseRequestInput(
                    branch_id=details["branch_id"],
                    book_id=details["book_id"],
                    quantity=details["quantity"],
                    notes=details.get("notes"),
                ),
                context,
            )
            return _pending_confirmation_state(state, result, "Preparar solicitud de compra interna simulada")

        if intent == "general_help":
            return {
                **state,
                "response": "Hola, soy BiblioBot. Puedo ayudarte con "
                f"{_describe_allowed_capabilities(state.get('permissions', []))}.",
                "state": ChatState.IDLE.value,
                "next_step": "WAITING_USER_MESSAGE",
                "metadata": {
                    **state.get("metadata", {}),
                    "suggestions": FRONTEND_ACTION_SERVICE.get_initial_suggestions(),
                },
            }

        return {
            **state,
            "response": "Puedo ayudarte con catalogo, disponibilidad, compras, facturas e inventario. Que necesitas revisar?",
            "state": ChatState.NEEDS_CLARIFICATION.value,
            "next_step": "ASK_CLARIFICATION",
            "metadata": {
                **state.get("metadata", {}),
                "suggestions": FRONTEND_ACTION_SERVICE.get_initial_suggestions(),
            },
        }

    return tool_dispatch_node


def make_response_builder_node(
    llm_assistant_service: LlmAssistantService,
) -> Callable[[ChatGraphState], ChatGraphState]:
    def response_builder_node(state: ChatGraphState) -> ChatGraphState:
        response = state.get("response") or "No pude preparar una respuesta segura. Intenta reformular tu solicitud."
        improved = llm_assistant_service.improve_response(response, state.get("message", ""), state.get("intent", "unknown"))
        return {**state, "response": improved or response}

    return response_builder_node


def final_safety_node(state: ChatGraphState) -> ChatGraphState:
    safe_state = _coerce_chat_state(state.get("state"))
    safe_ui_action = _coerce_ui_action(state.get("ui_action"))
    links = _safe_links(state.get("links", []))
    context = dict(state.get("context", {}))
    metadata = _safe_metadata(state.get("metadata", {}))
    intent = state.get("intent", context.get("intent", "unknown"))

    if intent in SENSITIVE_INTENTS and safe_state in {ChatState.DONE.value, ChatState.EXECUTING_ACTION.value}:
        safe_state = ChatState.WAITING_CONFIRMATION.value
        state = {
            **state,
            "response": "La accion quedo pendiente de confirmacion. No se ejecuto ningun cambio real.",
        }

    selected_book_id = context.get("selectedBookId")
    if safe_ui_action == UiActionType.NAVIGATE_TO_PRODUCT.value and not selected_book_id:
        has_book_link = any(_is_safe_internal_link(link.url) and link.type == "BOOK_DETAIL" for link in links)
        if not has_book_link:
            safe_ui_action = UiActionType.NONE.value

    if safe_ui_action == UiActionType.NAVIGATE_TO_CATALOG.value:
        metadata.setdefault("filters", {})
        if not isinstance(metadata["filters"], dict):
            metadata["filters"] = {}

    context.update(
        {
            "intent": intent,
            "requiresConfirmation": bool(state.get("requires_confirmation", False)),
            "actionRef": state.get("action_ref"),
            "saleOrigin": "CHATBOT",
            "nextAction": state.get("next_step"),
            "metadata": metadata,
        }
    )
    if selected_book_id:
        context["selectedBookId"] = selected_book_id

    return {
        **state,
        "state": safe_state,
        "ui_action": safe_ui_action,
        "links": links,
        "context": context,
        "metadata": metadata,
        "is_terminal": True,
    }


def _catalog_result_state(state: ChatGraphState, query: str | None, result: dict[str, Any]) -> ChatGraphState:
    if _is_backend_error(result):
        response = "No pude consultar el catalogo en este momento. Puedes intentarlo nuevamente en unos segundos."
        books = []
    else:
        books = result.get("books", []) if result.get("status") == "MOCK_ONLY" else []
        titles = [book["title"] for book in books[:3]]
        response = (
            "Claro, encontre algunos libros relacionados con tu busqueda. Te dejo el catalogo filtrado para revisarlos: "
            + "; ".join(titles)
            + "."
            if titles
            else "No encontre coincidencias por ahora. Puedes probar con otro titulo, autor o categoria."
        )
    filters = _catalog_filters(query)
    catalog_link = FRONTEND_ACTION_SERVICE.build_catalog_link(query, filters)
    return {
        **state,
        "response": response,
        "state": ChatState.INTENT_DETECTED.value,
        "ui_action": UiActionType.NAVIGATE_TO_CATALOG.value,
        "links": [catalog_link] if catalog_link else [],
        "next_step": "SEARCH_BOOKS_PENDING",
        "tool_result": result,
        "metadata": {
            **state.get("metadata", {}),
            **FRONTEND_ACTION_SERVICE.build_catalog_metadata(query, filters),
            "resultCount": len(books),
            "books": _summarize_books(books),
        },
    }


def _book_detail_result_state(state: ChatGraphState, result: dict[str, Any]) -> ChatGraphState:
    if _is_backend_error(result):
        return _asking_details(
            state,
            "No pude consultar el detalle del libro en este momento. Puedes intentarlo nuevamente en unos segundos.",
            "ASK_BOOK_IDENTIFIER",
        )
    book = result.get("book")
    if not book:
        return _asking_details(state, "No encontre ese libro. Indica otro titulo o identificador y lo reviso.", "ASK_BOOK_IDENTIFIER")
    link = FRONTEND_ACTION_SERVICE.build_book_detail_link(book["id"], book["title"])
    visual_metadata = FRONTEND_ACTION_SERVICE.build_book_metadata(book["id"], book["title"])
    return {
        **state,
        "response": "Encontre este libro. Te dejo el detalle para revisar su informacion, disponibilidad y precio.",
        "state": ChatState.INTENT_DETECTED.value,
        "ui_action": UiActionType.NAVIGATE_TO_PRODUCT.value,
        "links": [link],
        "next_step": "BOOK_DETAIL_READY",
        "tool_result": result,
        "context": {**state.get("context", {}), "selectedBookId": book["id"]},
        "metadata": {**state.get("metadata", {}), "book": _summarize_book(book), **visual_metadata},
    }


def _stock_result_state(state: ChatGraphState, result: dict[str, Any]) -> ChatGraphState:
    if _is_backend_error(result):
        return _asking_details(
            state,
            "No pude consultar la disponibilidad en este momento. Puedes intentarlo nuevamente en unos segundos.",
            "ASK_BOOK_AND_BRANCH",
        )
    stock = result.get("stock")
    if not stock:
        return _asking_details(state, "No encontre disponibilidad para ese libro. Revisa el titulo o la sede e intenta de nuevo.", "ASK_BOOK_AND_BRANCH")
    return {
        **state,
        "response": f"Encontre disponibilidad para {stock['title']}: {stock.get('totalStock', stock.get('stock', 0))} unidades en total.",
        "state": ChatState.INTENT_DETECTED.value,
        "next_step": "STOCK_CHECK_READY",
        "tool_result": result,
        "metadata": {**state.get("metadata", {}), "stock": stock},
    }


def _invoice_result_state(state: ChatGraphState, invoice_id: str, result: dict[str, Any]) -> ChatGraphState:
    if _is_backend_error(result):
        return {
            **state,
            "response": "No pude consultar esa factura en este momento. Puedes intentarlo nuevamente en unos segundos.",
            "state": ChatState.NEEDS_CLARIFICATION.value,
            "next_step": "ASK_INVOICE_OR_SALE_ID",
            "tool_result": result,
            "metadata": {**state.get("metadata", {}), "invoiceId": invoice_id, "invoice": None},
        }
    invoice = result.get("invoice")
    if not invoice:
        return {
            **state,
            "response": "No encontre esa factura. Verifica el numero o comparte el identificador de venta.",
            "state": ChatState.NEEDS_CLARIFICATION.value,
            "next_step": "ASK_INVOICE_OR_SALE_ID",
            "tool_result": result,
            "metadata": {**state.get("metadata", {}), "invoiceId": invoice_id, "invoice": None},
        }
    return {
        **state,
        "response": f"Encontre la factura {invoice['id']}. Total: {invoice['total']}. Estado: {invoice['status']}.",
        "state": ChatState.INTENT_DETECTED.value,
        "ui_action": UiActionType.SHOW_INVOICE.value,
        "links": [],
        "next_step": "INVOICE_READY",
        "tool_result": result,
        "context": {**state.get("context", {}), "invoiceNumber": invoice["id"], "saleId": invoice.get("saleId")},
        "metadata": {
            **state.get("metadata", {}),
            "invoice": invoice,
            "invoiceNumber": invoice["id"],
            "saleId": invoice.get("saleId"),
        },
    }


def _sales_result_state(state: ChatGraphState, result: dict[str, Any]) -> ChatGraphState:
    scope = result.get("scope", "own")
    count = result.get("resultCount", 0)
    response = (
        f"Encontre {count} venta para la consulta general."
        if scope == "all"
        else f"Encontre {count} venta asociada a tu consulta."
    )
    return {
        **state,
        "response": response,
        "state": ChatState.INTENT_DETECTED.value,
        "next_step": "QUERY_SALES_PENDING" if scope == "all" else "QUERY_OWN_SALES_PENDING",
        "tool_result": result,
        "metadata": {
            **state.get("metadata", {}),
            "scope": scope,
            "sales": result.get("sales", []),
            "resultCount": count,
        },
    }


def _pending_confirmation_state(
    state: ChatGraphState,
    result: dict[str, Any],
    summary: str,
    selected_book_id: str | None = None,
    metadata_extra: dict[str, Any] | None = None,
) -> ChatGraphState:
    pending_action = result.get("pendingAction")
    action_ref = result.get("actionRef")
    context = dict(state.get("context", {}))
    if selected_book_id:
        context["selectedBookId"] = selected_book_id
    return {
        **state,
        "response": f"Perfecto. Ya tengo preparada esta accion: {summary}. Antes de continuar, necesito que confirmes si deseas realizarla.",
        "state": ChatState.WAITING_CONFIRMATION.value,
        "next_step": "AWAIT_EXPLICIT_CONFIRMATION",
        "requires_confirmation": True,
        "action_ref": action_ref,
        "pending_action": pending_action,
        "tool_result": result,
        "context": context,
        "metadata": {**state.get("metadata", {}), **(metadata_extra or {}), "pendingAction": pending_action},
    }


def _sensitive_details_state(state: ChatGraphState, next_action: str, intent: str) -> ChatGraphState:
    messages = {
        "inventory_entry": "Indica libro, cantidad, sede y motivo de la entrada. No registrare nada sin tu confirmacion.",
        "transfer_request": "Indica libro, cantidad, sede origen y sede destino. No creare ningun traslado sin tu confirmacion.",
        "purchase_request": "Indica libro, cantidad y justificacion de la solicitud. No creare ninguna solicitud sin tu confirmacion.",
    }
    return {
        **state,
        "response": messages[intent],
        "state": ChatState.ASKING_DETAILS.value,
        "next_step": next_action,
        "requires_confirmation": True,
        "metadata": {
            **state.get("metadata", {}),
            "pendingAction": {"intent": intent, "status": "AWAITING_REQUIRED_DETAILS", "mockOnly": True},
        },
    }


def _asking_details(
    state: ChatGraphState,
    response: str,
    next_action: str,
    requires_confirmation: bool = False,
) -> ChatGraphState:
    return {
        **state,
        "response": response,
        "state": ChatState.ASKING_DETAILS.value,
        "next_step": next_action,
        "requires_confirmation": requires_confirmation,
    }


def _terminal_response(
    state: ChatGraphState,
    response: str,
    chat_state: ChatState,
    intent: str,
    next_action: str,
    metadata_extra: dict[str, Any] | None = None,
) -> ChatGraphState:
    metadata = {**state.get("metadata", {}), "detectedIntent": intent}
    if metadata_extra:
        metadata.update(metadata_extra)
    return {
        **state,
        "response": response,
        "state": chat_state.value,
        "intent": intent,
        "ui_action": UiActionType.NONE.value,
        "links": [],
        "metadata": metadata,
        "next_step": next_action,
        "is_terminal": True,
    }


def _auth_required_response(state: ChatGraphState, original_intent: str) -> ChatGraphState:
    metadata = {
        **state.get("metadata", {}),
        "detectedIntent": "auth_required",
        "originalIntent": original_intent,
        "authRequired": True,
        "guest": True,
    }
    return {
        **state,
        "response": _auth_required_message(original_intent),
        "state": ChatState.NEEDS_CLARIFICATION.value,
        "intent": "auth_required",
        "ui_action": UiActionType.NONE.value,
        "links": AUTH_REQUIRED_SERVICE.build_auth_links(),
        "metadata": metadata,
        "requires_confirmation": False,
        "action_ref": None,
        "pending_action": None,
        "tool_result": None,
        "next_step": "AUTH_REQUIRED",
        "is_terminal": True,
    }


def _tool_context(state: ChatGraphState) -> ToolExecutionContext:
    return ToolExecutionContext(
        session_id=state.get("session_id", ""),
        user_id=state.get("user_id"),
        roles=state.get("roles", []),
        permissions=state.get("permissions", []),
        source=state.get("source", "DOTNET_BACKEND"),
    )


def _is_guest_state(state: ChatGraphState) -> bool:
    return AUTH_REQUIRED_SERVICE.is_guest_context(
        state.get("roles", []),
        state.get("user_id"),
        state.get("permissions", []),
    )


def _detect_intent(normalized: str) -> str:
    if any(keyword in normalized for keyword in ("factura", "recibo", "comprobante")):
        return "invoice_query"
    if "muestrame" in normalized:
        if any(keyword in normalized for keyword in ("libros", "catalogo")) or any(
            genre in normalized for genre in GENRE_WORDS
        ):
            return "catalog_search"
        return "book_detail"
    if re.search(r"\bx\s*-?\d+\b", normalized):
        return "purchase_intent"

    priority_rules = [
        ("purchase_request", ("solicitud de compra", "pedir proveedor", "comprar inventario")),
        ("sales_query", ("ventas", "venta de hoy", "reporte de ventas", "mis ventas")),
        ("transfer_request", ("traslado", "mover sede", "transferir")),
        ("inventory_entry", ("inventario", "entrada", "registrar entrada")),
        ("stock_check", ("stock", "disponible", "disponibilidad", "existencias")),
        (
            "purchase_intent",
            (
                "comprar",
                "quiero llevar",
                "agrega",
                "agregar",
                "agregar al carrito",
                "anade",
                "anadir",
                "anadir al carrito",
            ),
        ),
        ("book_detail", ("detalle", "informacion del libro", "ver libro", "muestrame")),
        ("catalog_search", ("buscar", "catalogo", "libro", "libros", "tienen", "recomienda", "recomiendame", "recomendame")),
        ("general_help", ("hola", "ayuda", "que puedes hacer")),
    ]
    for intent, keywords in priority_rules:
        if any(keyword in normalized for keyword in keywords):
            return intent
    return "unknown"


def _allowed_llm_intents(permission_service: PermissionService) -> list[str]:
    return [intent for intent in permission_service.INTENT_PERMISSIONS if intent != "unknown"]


def _extract_catalog_query(message: str) -> str | None:
    normalized = _normalize(message)
    stop_words = {
        "buscar",
        "busco",
        "catalogo",
        "libro",
        "libros",
        "tienen",
        "tienes",
        "recomienda",
        "recomiendame",
        "recomendame",
        "muestrame",
        "quiero",
        "ver",
        "de",
        "del",
        "la",
        "el",
        "un",
        "una",
    }
    words = [word for word in normalized.split() if word not in stop_words]
    return " ".join(words) or None


def _extract_invoice_id(message: str) -> str | None:
    match = re.search(r"\bFAC-\d{4,}\b", message.upper())
    return match.group(0) if match else None


def _extract_quantity(message: str) -> int | None:
    normalized = _normalize(message)
    multiplier_match = re.search(r"\bx\s*(-?\d+)\b", normalized)
    if multiplier_match:
        quantity = int(multiplier_match.group(1))
        return quantity if quantity > 0 else None

    match = re.search(r"(?<![\w-])(-?\d+)\b", normalized)
    if match:
        quantity = int(match.group(1))
        return quantity if quantity > 0 else None
    quantity_words = {"un": 1, "una": 1, "uno": 1, "dos": 2, "tres": 3, "cuatro": 4, "cinco": 5}
    for word, quantity in quantity_words.items():
        if re.search(rf"\b{word}\b", normalized):
            return quantity
    return None


def _find_book_from_message(message: str, tool_service: BiblioBotToolService) -> dict | None:
    normalized = _normalize(message)
    for book in tool_service.mock_client.search_books():
        if _normalize(book["id"]) in normalized or _normalize(book["title"]) in normalized:
            return tool_service.mock_client.get_book_detail(book["id"])
    return None


def _enrich_purchase_pending_result(result: dict[str, Any], book: dict, quantity: int) -> dict[str, Any]:
    pending_action = result.get("pendingAction")
    if not isinstance(pending_action, dict):
        return result

    details = dict(pending_action.get("details") or {})
    details.update(
        {
            "bookId": book["id"],
            "bookTitle": book["title"],
            "quantity": quantity,
        }
    )
    enriched_pending_action = {**pending_action, "details": details}
    enriched_pending_action["bookId"] = book["id"]
    enriched_pending_action["bookTitle"] = book["title"]
    enriched_pending_action["quantity"] = quantity
    return {**result, "pendingAction": enriched_pending_action}


def _extract_sensitive_details(
    message: str,
    tool_service: BiblioBotToolService,
    needs_two_branches: bool = False,
    require_branch: bool = False,
) -> dict[str, Any] | None:
    book = _find_book_from_message(message, tool_service)
    quantity = _extract_quantity(message)
    branches = _extract_branches(message, tool_service)
    if not book or not quantity:
        return None
    if needs_two_branches:
        if len(branches) < 2:
            return None
        return {
            "book_id": book["id"],
            "quantity": quantity,
            "source_branch_id": branches[0],
            "destination_branch_id": branches[1],
            "notes": "Solicitud simulada desde chatbot.",
        }
    if require_branch and not branches:
        return None
    branch_id = branches[0] if branches else None
    return {
        "book_id": book["id"],
        "quantity": quantity,
        "branch_id": branch_id,
        "notes": "Solicitud simulada desde chatbot.",
    }


def _extract_branches(message: str, tool_service: BiblioBotToolService) -> list[str]:
    normalized = _normalize(message)
    found = []
    for branch in tool_service.mock_client.list_branches():
        branch_id = branch["id"]
        branch_name = _normalize(branch["name"])
        branch_alias = branch_id.replace("branch-", "")
        if _normalize(branch_id) in normalized or branch_name in normalized or branch_alias in normalized:
            found.append(branch_id)
    return found


def _catalog_filters(query: str | None) -> dict[str, str]:
    if not query:
        return {}
    normalized = _normalize(query)
    for genre in GENRE_WORDS:
        if genre in normalized:
            return {"genre": genre}
    return {"query": query}


def _base_metadata(request: ChatProcessRequest, intent: str) -> dict[str, Any]:
    guest = AUTH_REQUIRED_SERVICE.is_guest_context(request.roles, request.userId, request.permissions)
    return {
        "sessionId": request.sessionId,
        "source": request.source,
        "roles": request.roles,
        "permissions": request.permissions,
        "detectedIntent": intent,
        "guest": guest,
    }


def _summarize_books(books: list[dict]) -> list[dict]:
    return [_summarize_book(book) for book in books[:5]]


def _summarize_book(book: dict) -> dict:
    return {
        "id": book["id"],
        "title": book["title"],
        "author": book["author"],
        "genre": book["genre"],
        "price": book["price"],
        "available": book["available"],
    }


def _describe_allowed_capabilities(permissions: list[str]) -> str:
    service = PermissionService()
    capabilities = []
    if service.has_any_permission(permissions, ["books.read", "books.search"]):
        capabilities.append("buscar libros y revisar disponibilidad")
    if service.has_any_permission(permissions, ["cart.manage", "sales.create"]):
        capabilities.append("preparar compras con confirmacion")
    if service.has_any_permission(permissions, ["invoices.read_own", "invoices.read_all"]):
        capabilities.append("consultar facturas")
    if service.has_any_permission(permissions, ["sales.read_own", "sales.read_all"]):
        capabilities.append("consultar ventas")
    if service.has_any_permission(permissions, ["inventory.entry", "inventory.read"]):
        capabilities.append("revisar inventario")
    if service.has_any_permission(permissions, ["requests.transfer.create", "requests.purchase.create"]):
        capabilities.append("preparar solicitudes internas")
    return ", ".join(capabilities) if capabilities else "orientarte y explorar el catalogo"


def _permission_denied_message(intent: str) -> str:
    messages = {
        "catalog_search": "No puedo mostrar el catalogo porque tu usuario no tiene el permiso necesario.",
        "book_detail": "No puedo mostrar detalles de libros porque tu usuario no tiene el permiso necesario.",
        "stock_check": "No puedo consultar disponibilidad porque tu usuario no tiene el permiso necesario.",
        "purchase_intent": "No puedo preparar esa compra porque tu usuario no tiene el permiso necesario.",
        "invoice_query": "No puedo mostrar esa factura porque tu usuario no tiene el permiso necesario.",
        "sales_query": "No puedo mostrar esa informacion porque tu usuario no tiene el permiso necesario.",
        "inventory_entry": "No puedo preparar esa entrada de inventario porque tu usuario no tiene el permiso necesario.",
        "transfer_request": "No puedo preparar esa solicitud de traslado porque tu usuario no tiene el permiso necesario.",
        "purchase_request": "No puedo preparar esa solicitud de compra porque tu usuario no tiene el permiso necesario.",
    }
    return messages.get(intent, "No puedo realizar esa accion porque tu usuario no tiene el permiso necesario.")


def _auth_required_message(intent: str) -> str:
    if intent in {"purchase_intent", "cart_manage", "cart_read", "create_sale", "confirm_sale"}:
        return (
            "Puedo ayudarte a encontrar el libro, pero para comprar o usar el carrito "
            "necesitas iniciar sesion o crear una cuenta."
        )
    return (
        "Para continuar con esa accion necesitas iniciar sesion o crear una cuenta. "
        "Mientras tanto, puedo ayudarte a explorar el catalogo."
    )


def _is_backend_error(result: dict[str, Any]) -> bool:
    return result.get("status") in {"BACKEND_ERROR", "AUTH_REQUIRED", "PERMISSION_DENIED"} and "errorCode" in result


def _coerce_chat_state(value: Any) -> str:
    allowed = {item.value for item in ChatState}
    return value if value in allowed else ChatState.FAILED.value


def _coerce_ui_action(value: Any) -> str:
    allowed = {item.value for item in UiActionType}
    return value if value in allowed else UiActionType.NONE.value


def _safe_links(links: list[Any]) -> list[ChatLink]:
    safe = []
    for link in links:
        if not isinstance(link, ChatLink):
            continue
        if _is_safe_internal_link(link.url):
            safe.append(link)
    return safe


def _is_safe_internal_link(url: str) -> bool:
    return FRONTEND_ACTION_SERVICE.sanitize_internal_path(url) is not None


def _safe_metadata(metadata: dict[str, Any]) -> dict[str, Any]:
    blocked_keys = {"api_key", "apikey", "password", "secret", "jwt.secret", "connectionstring", "connection_string"}
    return {key: value for key, value in metadata.items() if key.lower() not in blocked_keys}


def _normalize(value: str) -> str:
    without_accents = "".join(
        char
        for char in unicodedata.normalize("NFD", value.lower())
        if unicodedata.category(char) != "Mn"
    )
    return " ".join(without_accents.split())
