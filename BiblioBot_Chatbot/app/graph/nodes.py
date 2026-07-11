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
from app.services.response_composer_service import ResponseComposerService
from app.tools.bibliobot_tools import BiblioBotToolService
from app.tools.tool_context import ToolExecutionContext
from app.tools.tool_schemas import (
    AddOrUpdateCartItemInput,
    CheckStockInput,
    ConfirmSaleInput,
    CreateSaleFromCartInput,
    RefineCatalogFilterInput,
    CreatePurchaseRequestInput,
    CreateTransferRequestInput,
    GetBookDetailInput,
    GetInvoiceInput,
    QuerySalesInput,
    RegisterInventoryEntryInput,
    SearchBooksInput,
)

from app.graph.state import ChatGraphState


SENSITIVE_INTENTS = {
    "purchase_intent",
    "checkout_cart",
    "confirm_sale",
    "inventory_entry",
    "transfer_request",
    "purchase_request",
    "admin_inventory_adjustment",
}
AUTH_REQUIRED_SERVICE = AuthRequiredService()
FRONTEND_ACTION_SERVICE = FrontendActionService()
LAST_BOOK_BY_SESSION: dict[str, dict[str, Any]] = {}
CONVERSATION_MEMORY_BY_SESSION: dict[str, dict[str, Any]] = {}
ALLOWED_INTENTS = [
    "catalog_search",
    "refine_catalog_filter",
    "book_detail",
    "stock_check",
    "list_categories",
    "purchase_intent",
    "checkout_cart",
    "confirm_sale",
    "invoice_query",
    "inventory_entry",
    "transfer_request",
    "purchase_request",
    "sales_query",
    "greeting",
    "identity_help",
    "stock_context_query",
    "stock_explicit_query",
    "admin_inventory_adjustment",
    "admin_navigation",
    "page_navigation",
    "general_help",
    "out_of_domain",
    "unknown",
]
GENRE_WORDS = {
    "arte",
    "arquitectura de software",
    "aventura",
    "ciencia",
    "clasicos",
    "desarrollo personal",
    "distopia",
    "ensayo",
    "fantasia",
    "ficcion",
    "filosofia",
    "terror",
    "romance",
    "ciencia ficcion",
    "ciencia ficciòn",
    "historia",
    "infantil",
    "infantiles",
    "ingenieria de software",
    "misterio",
    "negocios",
    "ninos",
    "politica",
    "programacion",
    "software",
    "tecnologia",
}
CATEGORY_SYNONYMS = {
    "arte": "arte",
    "arquitectura de software": "software",
    "aventura": "aventura",
    "autoayuda": "desarrollo personal",
    "ciencia": "ciencia",
    "clasico": "clasicos",
    "clasicos": "clasicos",
    "desarrollo": "desarrollo personal",
    "desarrollo personal": "desarrollo personal",
    "distopia": "distopia",
    "ensayo": "ensayo",
    "ficcion": "ficcion",
    "ficciones": "ficcion",
    "ciencia ficcion": "ciencia ficcion",
    "ciencia ficciòn": "ciencia ficcion",
    "fantasia": "fantasia",
    "fantacia": "fantasia",
    "filosofia": "filosofia",
    "historia": "historia",
    "ingenieria de software": "software",
    "negocio": "negocios",
    "negocios": "negocios",
    "misterio": "misterio",
    "ninos": "infantil",
    "ninoses": "infantil",
    "niños": "infantil",
    "infantil": "infantil",
    "infantiles": "infantil",
    "programacion": "programacion",
    "politica": "politica",
    "romance": "romance",
    "software": "programacion",
    "tecnologia": "tecnologia",
    "terror": "terror",
}
TECHNICAL_CATALOG_TERMS = {
    "arquitectura de software",
    "arquitectura software",
    "clean architecture",
    "clean code",
    "codigo limpio",
    "desarrollo de software",
    "design patterns",
    "ingenieria de software",
    "ingenieria software",
    "libros tecnicos",
    "programacion",
    "programar",
    "refactoring",
    "software",
    "tecnicos",
    "tecnico",
    "tecnologia",
    "the pragmatic programmer",
}
TECHNICAL_CATALOG_TARGETS = {
    "api",
    "apis",
    "arquitectura",
    "clean",
    "codigo",
    "desarrollo",
    "disenar",
    "ingenieria",
    "mantenible",
    "patrones",
    "programacion",
    "programar",
    "python",
    "refactoring",
    "software",
    "tecnologia",
}
CATEGORY_LIST_WORDS = {
    "categoria",
    "categorias",
    "categoria:",
    "categoría",
    "categorías",
    "libros de",
    "categoria de",
    "género",
    "genero",
}
CATEGORY_LIST_PHRASES = (
    "dime las categorias",
    "dime categorias",
    "dime cuales categorias hay",
    "cuales categorias hay",
    "que categorias hay",
    "muestrame las categorias",
    "lista las categorias",
    "categorias disponibles",
    "generos disponibles",
    "tipos de libros",
    "que tipos de libros hay",
)
COMMON_QUERY_CORRECTIONS = {
    "fantacia": "fantasia",
    "fantasia": "fantasia",
    "halo": "hola",
    "hoal": "hola",
    "hols": "hola",
    "holaa": "hola",
    "holaaa": "hola",
    "holaaaa": "hola",
    "ola": "hola",
    "q": "que",
    "k": "que",
    "kien": "quien",
    "categrias": "categorias",
    "categorias": "categorias",
    "infatil": "infantil",
    "infantiles": "infantil",
    "infantilies": "infantil",
    "programacion": "programacion",
}
NATURAL_BOOK_DETAIL_PHRASES = (
    "dame un resumen de",
    "dame resumen de",
    "dame un resumen",
    "resumen de",
    "resumen del libro",
    "sinopsis de",
    "sinopsis del libro",
    "descripcion de",
    "descripcion del libro",
    "resumeme",
    "me puedes resumir",
    "dime sobre",
    "dime de",
    "dime algo de",
    "dime algo sobre",
    "hablame de",
    "hablame sobre",
    "hablo de",
    "hablo del libro",
    "cuentame de",
    "cuentame sobre",
    "quiero saber de",
    "quiero saber sobre",
    "quiero saber acerca de",
    "quiero saber de que trata",
    "que sabes de",
    "informacion de",
    "informacion sobre",
    "info de",
    "info sobre",
    "de que trata",
    "cuentame de que trata",
    "quiero ver",
    "mostrar libro",
    "el libro",
    "abre",
    "me interesa",
    "quien escribio",
    "cuanto cuesta",
    "precio de",
    "categoria tiene",
    "que categoria tiene",
    "categoria es",
    "que categoria es",
)
STOCK_CHECK_PHRASES = (
    "tienes",
    "tienen",
    "hay",
    "existe",
    "lo tienes",
    "lo tienen",
    "esta disponible",
    "disponibilidad de",
    "hay stock de",
    "stock de",
    "quedan unidades de",
    "cuantos hay de",
)
CATALOG_SEARCH_PHRASES = (
    "recomiendame un libro",
    "recomendame un libro",
    "recomiendame libros",
    "recomendame libros",
    "dame una recomendacion",
    "dame recomendaciones",
    "recomendacion de libros",
    "busca",
    "buscar",
    "busco libros",
    "quiero libros",
    "quiero un libro",
    "quiero libro",
    "tienes libros",
    "tienen libros",
    "libros para",
    "libros infantiles",
    "libros de",
    "libros del autor",
    "libros parecidos a",
    "libros como",
    "muestrame libros",
)
PURCHASE_INTENT_PHRASES = (
    "quiero comprar",
    "comprar",
    "agregame",
    "agrega",
    "agregar",
    "anade",
    "anadir",
    "quiero llevar",
    "quiero llevarme",
    "pon",
    "mete",
    "compra",
    "dame uno",
    "lo compro",
    "lo quiero",
    "me llevo uno",
    "quiero ese",
    "quiero ese libro",
    "quiero comprarlo",
    "quiero uno",
)
CHECKOUT_CART_PHRASES = (
    "finalizar compra",
    "finalizar mi compra",
    "comprar lo del carrito",
    "comprar carrito",
    "pagar carrito",
    "generar pedido",
    "crear pedido",
    "terminar compra",
    "proceder al pago",
    "hacer pedido",
    "confirmar carrito",
)
CONFIRM_SALE_PHRASES = (
    "confirmar venta",
    "confirmar pedido",
    "confirmar compra",
    "confirmar la venta",
    "confirmar mi pedido",
    "confirmar mi compra",
    "finalizar venta",
    "completar pedido",
    "completar compra",
    "generar factura",
    "facturar pedido",
)
DOMAIN_SIGNAL_TERMS = {
    "autor",
    "autores",
    "bibliobot",
    "carrito",
    "catalogo",
    "categoria",
    "categorias",
    "compra",
    "comprar",
    "disponibilidad",
    "ejemplar",
    "ejemplares",
    "editorial",
    "editoriales",
    "factura",
    "facturas",
    "genero",
    "generos",
    "inventario",
    "libro",
    "libros",
    "pedido",
    "precio",
    "resumen",
    "sinopsis",
    "stock",
    "titulo",
    "venta",
    "ventas",
    "biblio",
    "leer",
    "novela",
}
EXPLICIT_OUT_OF_DOMAIN_PHRASES = (
    "hazme una receta",
    "dame una receta",
    "receta de",
    "tarea de matematicas",
    "tarea de matematica",
    "quien gano el partido",
    "quien gano partido",
    "hablame de politica",
    "hablame de politica",
    "dime un chisme",
    "cuentame un chisme",
    "dime una cancion",
    "canta una cancion",
    "programa una app",
    "crea una app",
    "algo que no sea de libros",
    "no sea de libros",
)
GREETING_PHRASES = (
    "hola",
    "buenas",
    "buenos dias",
    "buenas tardes",
    "buenas noches",
    "hey",
    "ey",
)
IDENTITY_HELP_PHRASES = (
    "quien eres",
    "que eres",
    "hola quien eres",
    "para que sirves",
    "que puedes hacer",
    "ayudame",
    "ayuda",
)
STOCK_CONTEXT_PHRASES = (
    "cuantos hay disponibles",
    "cuanto hay disponibles",
    "cuanto queda",
    "cuantos quedan",
    "cuantos libros hay",
    "cuantos hay en stock",
    "cuantas unidades hay",
    "y en stock",
    "en stock cuantos",
    "disponibilidad",
    "hay disponibles",
    "hay unidades",
    "cuantos tienes",
)
STOCK_EXPLICIT_PHRASES = (
    "cuantos libros de",
    "cuantos hay de",
    "stock de",
    "disponibilidad de",
    "cuantos quedan de",
    "cuantos tienes de",
)
ADMIN_INVENTORY_ADJUSTMENT_PHRASES = (
    "saca uno del stock",
    "saques uno del stock",
    "quita uno del stock",
    "descuenta uno del inventario",
    "sacar uno del stock",
    "sacar del stock",
    "baja el stock",
    "ajusta el inventario",
    "registra salida",
    "agrega al stock",
    "sube el stock",
)
ADMIN_NAVIGATION_PHRASES = (
    "quiero agregar un usuario",
    "crear usuario",
    "registrar usuario",
    "llevame a usuarios",
    "abre usuarios",
    "abrir usuarios",
    "quiero agregar un libro",
    "crear libro",
    "editar libro",
    "abrir inventario",
    "llevame a inventario",
    "ver ventas",
    "ver facturas",
    "ver reportes",
    "ver solicitudes internas",
    "revisar pedidos",
)


def normalize_input_node(state: ChatGraphState) -> ChatGraphState:
    request = state["request"]
    normalized_message = _normalize(request.message)
    return {
        **state,
        "page_context": _safe_page_context(request.pageContext),
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
        session_id = state.get("session_id", "")
        if confirmation_service.is_explicit_cancellation(message):
            pending_action = confirmation_service.consume_pending_action(session_id)
            metadata_extra = {}
            if pending_action:
                metadata_extra = {
                    "originalIntent": pending_action.get("originalIntent") or pending_action.get("intent"),
                    "actionRef": pending_action.get("actionRef"),
                    "cancelledAction": pending_action,
                }
            return _terminal_response(
                state,
                response="Listo, cancele la accion pendiente. No se realizo ningun cambio.",
                chat_state=ChatState.IDLE,
                intent="cancel_confirmation",
                next_action="WAITING_USER_MESSAGE",
                metadata_extra=metadata_extra,
            )
        if confirmation_service.is_explicit_confirmation(message):
            pending_action = confirmation_service.consume_pending_action(session_id)
            if pending_action:
                return _confirmed_pending_action_response(state, pending_action)
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
        pending_clarification = _get_pending_clarification(state)
        normalized_message = state.get("normalized_message", "")
        if _can_resolve_pending_book_clarification(state, pending_clarification):
            intent = str(pending_clarification.get("intent") or "book_detail")
        elif _is_catalog_position_followup(normalized_message, state) or _is_short_explicit_book_followup(normalized_message, state):
            intent = "book_detail"
        elif _is_contextual_purchase_intent(normalized_message, state):
            intent = "purchase_intent"
        elif _is_category_followup_request(normalized_message):
            intent = "refine_catalog_filter"
        elif _is_followup_recommendation_request(normalized_message) and _has_last_catalog_results(state):
            intent = "catalog_search"
        else:
            intent = _detect_intent(normalized_message)
            if intent == "unknown" and _contains_refine_catalog_request(normalized_message, state):
                intent = "refine_catalog_filter"
        if _is_explicit_out_of_domain_request(normalized_message):
            intent = "out_of_domain"
        if intent == "unknown" and not _has_domain_signal(normalized_message):
            intent = "out_of_domain"
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


def make_tool_dispatch_node(
    tool_service: BiblioBotToolService,
    confirmation_service: ConfirmationService,
) -> Callable[[ChatGraphState], ChatGraphState]:
    def tool_dispatch_node(state: ChatGraphState) -> ChatGraphState:
        intent = state.get("intent", "unknown")
        context = _tool_context(state)
        message = state.get("message", "")

        if intent == "greeting":
            return _greeting_result_state(state)

        if intent == "identity_help":
            return _identity_help_result_state(state)

        if intent == "admin_inventory_adjustment":
            return _admin_inventory_adjustment_result_state(state, tool_service)

        if intent == "admin_navigation":
            return _admin_navigation_result_state(state)

        if intent == "catalog_search":
            if _is_followup_recommendation_request(state.get("normalized_message", "")) and _has_last_catalog_results(state):
                return _followup_recommendation_result_state(state)
            query = _extract_catalog_query(message)
            search_query = _catalog_search_query(query)
            result = tool_service.search_books(SearchBooksInput(query=search_query), context)
            return _catalog_result_state(state, query, result)

        if intent == "refine_catalog_filter":
            query, filters = _build_catalog_filters_from_message(message, state)
            source = _get_catalog_books_for_refinement(
                state=state,
                tool_service=tool_service,
                context=context,
                query=query,
            )
            result = {
                "status": "REAL_BACKEND",
                "mode": "READ_ONLY",
                "query": query,
                "books": source,
                "resultCount": len(source),
            }
            return _catalog_result_state(state, query, result, filters=filters, explicit_query=query)

        if intent == "book_detail":
            book = _find_book_from_message(message, tool_service)
            if not book:
                last_book = _get_last_book_for_context(state, message, allow_pronouns=True) or _get_selected_book_from_page_context(state)
                if last_book:
                    book = tool_service.mock_client.get_book_detail(last_book["id"])
            if not book:
                return _asking_details(
                    state,
                    "Claro, de que libro quieres el resumen?",
                    "ASK_BOOK_IDENTIFIER",
                )
            result = tool_service.get_book_detail(GetBookDetailInput(book_id=book["id"]), context)
            if _contains_stock_check_intent(state.get("normalized_message", "")):
                stock_result = tool_service.check_stock(CheckStockInput(book_id=book["id"]), context)
                return _book_detail_result_state(state, result, stock_result, _is_summary_or_info_request(message))
            return _book_detail_result_state(state, result, summary_requested=_is_summary_or_info_request(message))

        if intent == "stock_check":
            book = _find_book_from_message(message, tool_service)
            if not book:
                last_book = _get_last_book_for_context(state, message, allow_pronouns=True) or _get_selected_book_from_page_context(state)
                if last_book:
                    book = tool_service.mock_client.get_book_detail(last_book["id"])
            if not book:
                return _asking_details(
                    state,
                    "Indica el libro y, si aplica, la sede para revisar disponibilidad.",
                    "ASK_BOOK_AND_BRANCH",
                )
            result = tool_service.check_stock(CheckStockInput(book_id=book["id"]), context)
            return _stock_result_state(state, result)

        if intent in {"stock_context_query", "stock_explicit_query"}:
            book = None
            if intent == "stock_context_query":
                last_book = (
                    _get_last_book_for_context(state, message, allow_pronouns=True)
                    or _get_last_book_from_memory(state)
                    or _get_selected_book_from_page_context(state)
                )
                if last_book:
                    book = tool_service.mock_client.get_book_detail(last_book["id"])
            if not book:
                book = _find_book_from_stock_message(message, tool_service)
            if not book:
                return _asking_details(
                    state,
                    "Dime el titulo del libro y reviso su disponibilidad.",
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

        if intent == "list_categories":
            result = tool_service.list_categories(context)
            return _categories_result_state(state, result)

        if intent == "out_of_domain":
            return _out_of_domain_result_state(state)

        if intent == "checkout_cart":
            result = tool_service.create_sale_from_cart(
                CreateSaleFromCartInput(
                    session_id=state.get("session_id", ""),
                    origin_code="CHATBOT",
                ),
                context,
            )
            return _pending_confirmation_state(
                state,
                result,
                "Crear venta pendiente desde carrito",
                confirmation_service,
                metadata_extra={"originCode": "CHATBOT"},
            )

        if intent == "confirm_sale":
            sale_id = _extract_sale_id(message)
            branches = _extract_branches(message, tool_service)
            result = tool_service.confirm_sale(
                ConfirmSaleInput(
                    sale_id=sale_id,
                    branch_id=branches[0] if branches else None,
                ),
                context,
            )
            return _pending_confirmation_state(
                state,
                result,
                f"Confirmar venta {sale_id}" if sale_id else "Confirmar venta pendiente",
                confirmation_service,
                metadata_extra={"saleId": sale_id, "branchId": branches[0] if branches else None},
            )

        if intent == "purchase_intent":
            book = _find_book_from_message(message, tool_service)
            if not book:
                last_book = (
                    _get_last_book_for_context(state, message, allow_pronouns=True)
                    or (_get_last_book_from_memory(state) if _is_contextual_purchase_intent(state.get("normalized_message", ""), state) else None)
                    or _get_selected_book_from_page_context(state)
                )
                if last_book:
                    book = tool_service.mock_client.get_book_detail(last_book["id"])
            quantity = _extract_quantity(message)
            if book and quantity is None and (
                _purchase_can_default_to_one(message) or _is_contextual_purchase_intent(state.get("normalized_message", ""), state)
            ):
                quantity = 1
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
            _remember_last_book(state, book)
            return _pending_confirmation_state(
                state,
                _enrich_purchase_pending_result(result, book, quantity),
                f"Preparar compra de {quantity} unidad(es) de {book['title']}",
                confirmation_service,
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
            return _pending_confirmation_state(state, result, "Preparar entrada de inventario simulada", confirmation_service)

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
            return _pending_confirmation_state(state, result, "Preparar solicitud de traslado simulada", confirmation_service)

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
            return _pending_confirmation_state(state, result, "Preparar solicitud de compra interna simulada", confirmation_service)

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
    response_composer_service: ResponseComposerService | None = None,
) -> Callable[[ChatGraphState], ChatGraphState]:
    composer = response_composer_service or ResponseComposerService()

    def response_builder_node(state: ChatGraphState) -> ChatGraphState:
        response = composer.compose(state)
        if _should_skip_llm_response_improvement(state):
            return {**state, "response": response}
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
    page_context = state.get("page_context")

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
            "pageContext": _safe_page_context(page_context),
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


def _catalog_result_state(
    state: ChatGraphState,
    query: str | None,
    result: dict[str, Any],
    filters: dict[str, str] | None = None,
    explicit_query: str | None = None,
) -> ChatGraphState:
    if _is_backend_error(result):
        response = "No pude consultar el catalogo en este momento. Puedes intentarlo nuevamente en unos segundos."
        books = []
    else:
        books = result.get("books", [])
        if not isinstance(books, list):
            books = []
        catalog_filters = filters or _catalog_filters(query)
        books = _filter_books_by_catalog_filters(books, catalog_filters)
        titles = [book.get("title", "") for book in books[:3] if isinstance(book, dict) and book.get("title")]
        response = (
            "Claro, encontre algunos libros relacionados con tu busqueda. Te dejo el catalogo filtrado para revisarlos: "
            + "; ".join(titles)
            + "."
            if titles
            else "No encontre coincidencias por ahora. Puedes probar con otro titulo, autor o categoria."
        )
    catalog_filters = filters or _catalog_filters(explicit_query or query)
    catalog_link = FRONTEND_ACTION_SERVICE.build_catalog_link(explicit_query or query, catalog_filters)
    if len(books) == 1 and isinstance(books[0], dict):
        _remember_last_book(state, books[0])
    _remember_last_catalog(state, explicit_query or query, catalog_filters, books)
    _remember_last_intent(state, "catalog_search", "SEARCH_BOOKS_PENDING")
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
            **FRONTEND_ACTION_SERVICE.build_catalog_metadata(explicit_query or query, catalog_filters),
            "resultCount": len(books),
            "filteredCount": len(books),
            "resultBooks": _summarize_books(books),
            "books": _summarize_books(books),
        },
    }


def _categories_result_state(state: ChatGraphState, result: dict[str, Any]) -> ChatGraphState:
    if _is_backend_error(result):
        return _asking_details(
            state,
            "No pude consultar las categorias en este momento. Puedes intentarlo nuevamente en unos segundos.",
            "ASK_CATEGORY_QUERY",
        )

    categories = result.get("categories", [])
    if not isinstance(categories, list):
        categories = []

    shown_categories = _clean_category_names(categories, limit=15)
    response = (
        "Estas son algunas categorias disponibles: " + ", ".join(shown_categories) + ". Puedes pedirme libros de una categoria especifica."
        if shown_categories
        else "Todavia no encontre categorias disponibles en el catalogo."
    )
    _remember_last_intent(state, "list_categories", "CATEGORIES_READY")
    return {
        **state,
        "response": response,
        "state": ChatState.INTENT_DETECTED.value,
        "ui_action": UiActionType.NAVIGATE_TO_CATALOG.value,
        "links": [],
        "next_step": "CATEGORIES_READY",
        "tool_result": result,
        "metadata": {
            **state.get("metadata", {}),
            "categories": shown_categories,
            "resultCount": len(categories),
        },
    }


def _greeting_result_state(state: ChatGraphState) -> ChatGraphState:
    return {
        **state,
        "response": "Hola, soy BiblioBot. Estoy aqui para ayudarte dentro de la biblioteca.",
        "state": ChatState.IDLE.value,
        "ui_action": UiActionType.NONE.value,
        "links": [],
        "next_step": "WAITING_USER_MESSAGE",
        "metadata": {
            **state.get("metadata", {}),
            "detectedIntent": "greeting",
            "suggestions": FRONTEND_ACTION_SERVICE.get_initial_suggestions(),
        },
    }


def _identity_help_result_state(state: ChatGraphState) -> ChatGraphState:
    return {
        **state,
        "response": "Soy BiblioBot, el copiloto de esta biblioteca virtual. Te ayudo con libros, catalogo, disponibilidad, compras y tareas administrativas si tienes permisos.",
        "state": ChatState.IDLE.value,
        "ui_action": UiActionType.NONE.value,
        "links": [],
        "next_step": "WAITING_USER_MESSAGE",
        "metadata": {
            **state.get("metadata", {}),
            "detectedIntent": "identity_help",
            "suggestions": FRONTEND_ACTION_SERVICE.get_initial_suggestions(),
        },
    }


def _out_of_domain_result_state(state: ChatGraphState) -> ChatGraphState:
    return {
        **state,
        "response": "Puedo ayudarte dentro de BiblioBot con catalogo, libros, disponibilidad, carrito, ventas, facturas e inventario.",
        "state": ChatState.NEEDS_CLARIFICATION.value,
        "ui_action": UiActionType.NONE.value,
        "links": [],
        "next_step": "OUT_OF_DOMAIN",
        "tool_result": None,
        "metadata": {
            **state.get("metadata", {}),
            "detectedIntent": "out_of_domain",
            "domainGuardrail": True,
            "resultCount": 0,
            "books": [],
        },
    }


def _admin_inventory_adjustment_result_state(
    state: ChatGraphState,
    tool_service: BiblioBotToolService,
) -> ChatGraphState:
    message = state.get("message", "")
    book = _find_book_from_stock_message(message, tool_service)
    if not book:
        last_book = _get_last_book_for_context(state, message, allow_pronouns=True) or _get_selected_book_from_page_context(state)
        if last_book:
            book = tool_service.mock_client.get_book_detail(last_book["id"])

    adjustment = _extract_inventory_adjustment_details(message)
    metadata_extra = {
        "safeMutationAvailable": False,
        "requiresPanelConfirmation": True,
        **adjustment,
    }
    if book:
        metadata_extra.update({"bookId": book.get("id"), "bookTitle": book.get("title")})
        _remember_last_book(state, book)
    _remember_last_admin_module(state, "inventory")

    link = FRONTEND_ACTION_SERVICE.build_admin_link(
        "Abrir inventario",
        FRONTEND_ACTION_SERVICE.ADMIN_INVENTORY_ROUTE,
        "ADMIN_INVENTORY",
    )
    return {
        **state,
        "response": "Puedo ayudarte con ese ajuste. Te llevo al modulo de inventario para revisarlo y confirmarlo desde el panel.",
        "state": ChatState.INTENT_DETECTED.value,
        "ui_action": UiActionType.NAVIGATE_TO_INVENTORY_ADJUSTMENT.value,
        "links": [link],
        "next_step": "NAVIGATE_TO_INVENTORY_ADJUSTMENT",
        "context": {**state.get("context", {}), "selectedBookId": book.get("id") if book else None},
        "metadata": {
            **state.get("metadata", {}),
            "detectedIntent": "admin_inventory_adjustment",
            **FRONTEND_ACTION_SERVICE.build_admin_metadata(
                FRONTEND_ACTION_SERVICE.ADMIN_INVENTORY_ROUTE,
                "inventory_adjustment",
                metadata_extra,
            ),
        },
    }


def _admin_navigation_result_state(state: ChatGraphState) -> ChatGraphState:
    target = _admin_navigation_target(state.get("normalized_message", ""))
    if not target:
        return _asking_details(state, "Dime a que modulo administrativo quieres ir.", "ASK_ADMIN_MODULE")

    if not _can_access_admin_target(state, target):
        return _terminal_response(
            state,
            response=_permission_denied_message("admin_navigation"),
            chat_state=ChatState.FAILED,
            intent="admin_navigation",
            next_action="PERMISSION_DENIED",
            metadata_extra={"adminTarget": target["target"], "requiredPermissions": target["permissions"]},
        )

    _remember_last_admin_module(state, target["target"])
    link = FRONTEND_ACTION_SERVICE.build_admin_link(target["label"], target["route"], target["linkType"])
    return {
        **state,
        "response": target["response"],
        "state": ChatState.INTENT_DETECTED.value,
        "ui_action": target["uiAction"],
        "links": [link],
        "next_step": target["uiAction"],
        "metadata": {
            **state.get("metadata", {}),
            "detectedIntent": "admin_navigation",
            **FRONTEND_ACTION_SERVICE.build_admin_metadata(
                target["route"],
                target["target"],
                {"requiredPermissions": target["permissions"]},
            ),
        },
    }


def _followup_recommendation_result_state(state: ChatGraphState) -> ChatGraphState:
    memory = _get_conversation_memory(state)
    last_catalog = _get_last_catalog_for_context(state)
    books = last_catalog.get("books") if isinstance(last_catalog, dict) else None
    if not isinstance(books, list) or not books:
        books = memory.get("lastCatalogResults") if isinstance(memory.get("lastCatalogResults"), list) else []

    if not books:
        return {
            **state,
            "response": "No tengo una busqueda anterior para continuar. Dime que tipo de libro quieres y busco opciones.",
            "state": ChatState.NEEDS_CLARIFICATION.value,
            "ui_action": UiActionType.NONE.value,
            "next_step": "ASK_CATALOG_QUERY",
            "metadata": {**state.get("metadata", {}), "resultCount": 0, "books": []},
        }

    next_index = _safe_recommendation_index(memory.get("lastRecommendationIndex"))
    if next_index >= len(books):
        return {
            **state,
            "response": "Ya te mostre los principales de esa busqueda. Puedo buscar otra categoria si quieres.",
            "state": ChatState.INTENT_DETECTED.value,
            "ui_action": UiActionType.NONE.value,
            "next_step": "SEARCH_RESULTS_EXHAUSTED",
            "metadata": {
                **state.get("metadata", {}),
                "query": last_catalog.get("query"),
                "filters": last_catalog.get("filters") if isinstance(last_catalog.get("filters"), dict) else {},
                "resultCount": len(books),
                "books": books,
                "lastRecommendationIndex": next_index,
            },
        }

    book = books[next_index]
    memory["lastRecommendationIndex"] = next_index + 1
    _remember_last_book(state, book)

    author = book.get("author") or _join_book_values(book.get("authors")) or "autor no especificado"
    genre = book.get("genre") or _join_book_values(book.get("categories")) or "catalogo"
    price = book.get("price")
    available = book.get("available")
    price_text = f", cuesta ${price}" if price is not None else ""
    availability_text = " y esta disponible" if available is True else " y no aparece disponible" if available is False else ""
    link = FRONTEND_ACTION_SERVICE.build_book_detail_link(str(book["id"]), str(book["title"]))

    return {
        **state,
        "response": f"Claro, otra opcion de {genre} es {book['title']}, de {author}{price_text}{availability_text}.",
        "state": ChatState.INTENT_DETECTED.value,
        "ui_action": UiActionType.NAVIGATE_TO_PRODUCT.value,
        "links": [link],
        "next_step": "BOOK_RECOMMENDATION_READY",
        "context": {**state.get("context", {}), "selectedBookId": book["id"]},
        "metadata": {
            **state.get("metadata", {}),
            "query": last_catalog.get("query"),
            "filters": last_catalog.get("filters") if isinstance(last_catalog.get("filters"), dict) else {},
            "resultCount": len(books),
            "book": book,
            "books": [book],
            "lastRecommendationIndex": memory["lastRecommendationIndex"],
        },
    }


def _book_detail_result_state(
    state: ChatGraphState,
    result: dict[str, Any],
    stock_result: dict[str, Any] | None = None,
    summary_requested: bool = False,
) -> ChatGraphState:
    if _is_backend_error(result):
        return _asking_details(
            state,
            "No pude consultar el detalle del libro en este momento. Puedes intentarlo nuevamente en unos segundos.",
            "ASK_BOOK_IDENTIFIER",
        )
    book = result.get("book")
    if not book:
        return _asking_details(state, "No encontre ese libro. Indica otro titulo o identificador y lo reviso.", "ASK_BOOK_IDENTIFIER")
    _remember_last_book(state, book)
    _clear_pending_clarification(state)
    _remember_last_intent(state, "book_detail", "BOOK_DETAIL_READY")
    link = FRONTEND_ACTION_SERVICE.build_book_detail_link(book["id"], book["title"])
    visual_metadata = FRONTEND_ACTION_SERVICE.build_book_metadata(book["id"], book["title"])
    stock = stock_result.get("stock") if isinstance(stock_result, dict) else None
    availability_text = ""
    if isinstance(stock, dict):
        total_stock = stock.get("totalStock", stock.get("stock", 0))
        availability_text = f" Disponibilidad actual: {total_stock} unidad(es)."
    response = _build_book_summary_response(book, stock) if summary_requested else (
        "Encontre este libro. Te dejo el detalle para revisar su informacion, disponibilidad y precio." + availability_text
    )
    return {
        **state,
        "response": response,
        "state": ChatState.INTENT_DETECTED.value,
        "ui_action": UiActionType.NAVIGATE_TO_PRODUCT.value,
        "links": [link],
        "next_step": "BOOK_DETAIL_READY",
        "tool_result": result,
        "context": {**state.get("context", {}), "selectedBookId": book["id"]},
        "metadata": {
            **state.get("metadata", {}),
            "book": _summarize_book(book),
            "stock": stock,
            "availabilityRequested": stock is not None,
            "summaryRequested": summary_requested,
            **visual_metadata,
        },
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
    _remember_last_book(state, {"id": stock.get("bookId"), "title": stock.get("title")})
    _clear_pending_clarification(state)
    _remember_last_intent(state, "stock_check", "STOCK_CHECK_READY")
    _remember_last_stock(state, stock)
    return {
        **state,
        "response": f"Encontre disponibilidad para {stock['title']}: {stock.get('totalStock', stock.get('stock', 0))} unidades en total.",
        "state": ChatState.INTENT_DETECTED.value,
        "next_step": "STOCK_CHECK_READY",
        "tool_result": result,
        "metadata": {**state.get("metadata", {}), "stock": stock},
    }


def _remember_last_book(state: ChatGraphState, book: dict[str, Any] | None) -> None:
    session_id = _session_key(state.get("session_id", ""))
    book_id = str((book or {}).get("id") or "").strip()
    if not session_id or not book_id:
        return

    LAST_BOOK_BY_SESSION[session_id] = {
        "id": book_id,
        "title": str((book or {}).get("title") or "").strip(),
        "book": dict(book or {}),
    }
    memory = CONVERSATION_MEMORY_BY_SESSION.setdefault(session_id, {})
    stock_by_branch = (book or {}).get("stockByBranch") or {}
    total_stock = sum(stock_by_branch.values()) if isinstance(stock_by_branch, dict) else None
    memory["lastBook"] = {
        "id": book_id,
        "title": str((book or {}).get("title") or "").strip(),
        "author": (book or {}).get("author") or _join_book_values((book or {}).get("authors")),
        "genre": (book or {}).get("genre") or _join_book_values((book or {}).get("categories")),
        "price": (book or {}).get("price"),
        "available": (book or {}).get("available"),
        "totalStock": total_stock,
    }


def _get_last_book_for_context(
    state: ChatGraphState,
    message: str,
    allow_pronouns: bool = False,
) -> dict[str, Any] | None:
    if _has_explicit_book_query(message):
        return None

    if not _is_contextual_book_reference(message, allow_pronouns=allow_pronouns):
        return None

    session_id = _session_key(state.get("session_id", ""))
    if not session_id:
        return None

    catalog_position = _extract_catalog_position_reference(_normalize(message))
    if catalog_position is not None:
        last_catalog = _get_last_catalog_for_context(state)
        catalog_books = last_catalog.get("books") if isinstance(last_catalog, dict) else None
        if isinstance(catalog_books, list) and 0 <= catalog_position < len(catalog_books):
            return catalog_books[catalog_position]

    contextual_book = _get_selected_book_from_page_context(state)
    if contextual_book:
        return contextual_book

    return LAST_BOOK_BY_SESSION.get(session_id)


def _get_last_book_from_memory(state: ChatGraphState) -> dict[str, Any] | None:
    session_id = _session_key(state.get("session_id", ""))
    stored = LAST_BOOK_BY_SESSION.get(session_id)
    if isinstance(stored, dict):
        if isinstance(stored.get("book"), dict):
            return stored["book"]
        if stored.get("id"):
            return stored

    memory = _get_conversation_memory(state)
    last_book = memory.get("lastBook") if memory else None
    if isinstance(last_book, dict) and last_book.get("id"):
        return last_book
    return None


def _get_selected_book_from_page_context(state: ChatGraphState) -> dict[str, Any] | None:
    page_context = state.get("page_context")
    if not isinstance(page_context, dict):
        return None

    selected_book = page_context.get("selectedBook")
    if not isinstance(selected_book, dict):
        return None

    if not selected_book.get("id"):
        return None

    return {
        "id": str(selected_book.get("id") or "").strip(),
        "title": str(selected_book.get("title") or "").strip(),
        "author": selected_book.get("author") or _join_book_values(selected_book.get("authors")),
        "genre": selected_book.get("genre") or _join_book_values(selected_book.get("categories")),
        "price": selected_book.get("price"),
        "available": selected_book.get("available"),
    }


def _get_visible_books_from_page_context(state: ChatGraphState) -> list[dict[str, Any]]:
    page_context = state.get("page_context")
    if not isinstance(page_context, dict):
        return []

    visible_books = page_context.get("visibleBooks")
    if not isinstance(visible_books, list):
        return []

    books: list[dict[str, Any]] = []
    for book in visible_books:
        if not isinstance(book, dict):
            continue

        book_id = str(book.get("id") or "").strip()
        title = str(book.get("title") or "").strip()
        if not book_id and not title:
            continue

        books.append(
            {
                "id": book_id,
                "title": title,
                "author": book.get("author") or _join_book_values(book.get("authors")),
                "genre": book.get("genre") or _join_book_values(book.get("categories")),
                "categories": book.get("categories") if isinstance(book.get("categories"), list) else [],
                "price": book.get("price"),
                "available": book.get("available"),
            }
        )
    return books


def _extract_catalog_position_reference(normalized_message: str) -> int | None:
    if not normalized_message:
        return None

    position_aliases: dict[str, int] = {
        "primer": 0,
        "primero": 0,
        "primera": 0,
        "segundo": 1,
        "segunda": 1,
        "tercero": 2,
        "tercera": 2,
        "cuarto": 3,
        "cuarta": 3,
        "quinto": 4,
        "quinta": 4,
    }

    for token, index in position_aliases.items():
        if token in normalized_message:
            return index
    return None


def _get_last_catalog_for_context(state: ChatGraphState) -> dict[str, Any]:
    memory = _get_conversation_memory(state)
    value = memory.get("lastCatalog")
    return value if isinstance(value, dict) else {}


def _has_last_catalog_results(state: ChatGraphState) -> bool:
    last_catalog = _get_last_catalog_for_context(state)
    books = last_catalog.get("books") if isinstance(last_catalog, dict) else None
    if isinstance(books, list) and bool(books):
        return True

    memory = _get_conversation_memory(state)
    return isinstance(memory.get("lastCatalogResults"), list) and bool(memory["lastCatalogResults"])


def _safe_recommendation_index(value: Any) -> int:
    return value if isinstance(value, int) and value >= 0 else 0


def _is_catalog_position_followup(normalized_message: str, state: ChatGraphState) -> bool:
    return _extract_catalog_position_reference(normalized_message) is not None and _has_last_catalog_results(state)


def _is_short_explicit_book_followup(normalized_message: str, state: ChatGraphState) -> bool:
    if not _get_conversation_memory(state).get("lastBook"):
        return False

    if not normalized_message.startswith(("y ", "sobre ", "y sobre ")):
        return False

    return _has_explicit_book_query(normalized_message)


def _is_contextual_book_reference(message: str, allow_pronouns: bool = False) -> bool:
    normalized = _normalize(message)
    contextual_markers = (
        "este libro",
        "ese libro",
        "el libro",
        "ese",
        "este",
        "el anterior",
        "el primer",
        "el primero",
        "la primera",
        "el segundo",
        "la segunda",
        "el tercero",
        "la tercera",
        "el cuarto",
        "la cuarta",
        "el quinto",
        "la quinta",
        "comprarlo",
        "comprar lo",
        "del libro",
        "dame un resumen de que trata",
        "dame resumen de que trata",
        "de que trata",
        "resumen de este libro",
        "resumen del libro",
    )
    has_contextual_marker = any(marker in normalized for marker in contextual_markers)
    return has_contextual_marker and (allow_pronouns or _is_summary_or_info_request(normalized))


def _get_conversation_memory(state: ChatGraphState) -> dict[str, Any]:
    session_id = _session_key(state.get("session_id", ""))
    if not session_id:
        return {}

    return CONVERSATION_MEMORY_BY_SESSION.setdefault(session_id, {})


def _remember_last_intent(state: ChatGraphState, intent: str, next_action: str | None) -> None:
    memory = _get_conversation_memory(state)
    if memory is not None:
        memory["lastIntent"] = {"intent": intent, "nextAction": next_action}


def _remember_last_stock(state: ChatGraphState, stock: dict[str, Any]) -> None:
    memory = _get_conversation_memory(state)
    if memory is not None:
        memory["lastStockResult"] = dict(stock)


def _remember_last_admin_module(state: ChatGraphState, module: str) -> None:
    memory = _get_conversation_memory(state)
    if memory is not None:
        memory["lastAdminModule"] = module


def _remember_last_catalog(
    state: ChatGraphState,
    query: str | None,
    filters: dict[str, str],
    books: list[dict],
) -> None:
    memory = _get_conversation_memory(state)
    if memory is not None:
        summarized_books = _summarize_books(books, limit=20)
        initial_recommendation_index = min(3, len(summarized_books))
        memory["lastCatalog"] = {
            "query": query,
            "filters": filters,
            "resultCount": len(summarized_books),
            "books": summarized_books,
        }
        memory["lastCatalogQuery"] = query
        memory["lastCatalogFilters"] = filters
        memory["lastCatalogResults"] = summarized_books
        memory["lastCatalogQueryState"] = {
            "query": query,
            "filters": filters,
            "resultCount": len(summarized_books),
            "books": summarized_books,
        }
        memory["lastRecommendationIndex"] = initial_recommendation_index
        memory["lastFilters"] = filters
        memory["lastCategory"] = filters.get("genre") or ""


def _set_pending_clarification(
    state: ChatGraphState,
    intent: str,
    expected_entity: str,
) -> dict[str, Any] | None:
    memory = _get_conversation_memory(state)
    if not memory and not _session_key(state.get("session_id", "")):
        return None

    pending_clarification = {
        "intent": intent,
        "expectedEntity": expected_entity,
        "originalMessage": state.get("message", ""),
    }
    memory["pendingClarification"] = pending_clarification
    return pending_clarification


def _get_pending_clarification(state: ChatGraphState) -> dict[str, Any] | None:
    memory = _get_conversation_memory(state)
    pending = memory.get("pendingClarification") if memory else None
    return pending if isinstance(pending, dict) else None


def _clear_pending_clarification(state: ChatGraphState) -> None:
    memory = _get_conversation_memory(state)
    if memory:
        memory.pop("pendingClarification", None)


def _can_resolve_pending_book_clarification(
    state: ChatGraphState,
    pending_clarification: dict[str, Any] | None,
) -> bool:
    if not pending_clarification or pending_clarification.get("expectedEntity") != "book":
        return False

    normalized = state.get("normalized_message", "")
    if not normalized:
        return False

    protected_markers = (
        "comprar",
        "agrega",
        "agregar",
        "anade",
        "anadir",
        "finalizar",
        "pagar",
        "generar",
        "facturar",
        "crear",
        "terminar",
        "proceder",
        "hacer",
        "completar",
        "confirmar",
        "pedido",
        "compra",
        "carrito",
        "factura",
        "stock",
        "disponibilidad",
        "disponible",
        "venta",
        "ventas",
        "inventario",
        "traslado",
        "solicitud",
        "reporte",
        "hola",
        "ayuda",
    )
    if any(_contains_normalized_phrase(normalized, marker) for marker in protected_markers):
        return False

    if normalized.startswith(("de ", "sobre ", "el libro ")):
        return True

    contextual_references = {"ese", "este", "ese libro", "este libro", "el anterior", "el primero", "el segundo"}
    if normalized in contextual_references:
        return True

    words = normalized.split()
    if not 1 <= len(words) <= 4:
        return False

    filler_words = {"de", "del", "el", "la", "un", "una", "libro", "sobre"}
    meaningful_words = [word for word in words if word not in filler_words]
    return bool(meaningful_words)


def _session_key(session_id: str) -> str:
    return " ".join(str(session_id or "").split())


def _build_book_summary_response(book: dict[str, Any], stock: dict[str, Any] | None = None) -> str:
    title = str(book.get("title") or "Este libro").strip()
    author = book.get("author") or _join_book_values(book.get("authors")) or "autor no especificado"
    category = book.get("genre") or _join_book_values(book.get("categories")) or "categoria no especificada"
    price = book.get("price")
    description = str(book.get("description") or "").strip()
    total_stock = None
    if isinstance(stock, dict):
        total_stock = stock.get("totalStock", stock.get("stock"))

    parts = [f"{title} es un libro de {author} dentro de {category}."]
    if description:
        parts.append(description)
    if price is not None:
        parts.append(f"Precio: ${price}.")
    if total_stock is not None:
        parts.append(f"Disponibilidad actual: {total_stock} unidad(es).")
    parts.append("Puedes abrir el detalle para revisar mas informacion.")
    return " ".join(parts)


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
    confirmation_service: ConfirmationService,
    selected_book_id: str | None = None,
    metadata_extra: dict[str, Any] | None = None,
) -> ChatGraphState:
    pending_action = result.get("pendingAction")
    action_ref = result.get("actionRef")
    context = dict(state.get("context", {}))
    if selected_book_id:
        context["selectedBookId"] = selected_book_id
    if pending_action:
        pending_action = {
            **pending_action,
            "originalIntent": state.get("intent", pending_action.get("intent")),
            "actionRef": action_ref or pending_action.get("actionRef"),
            "summary": summary,
        }
        if selected_book_id:
            pending_action["selectedBookId"] = selected_book_id
        result = {**result, "pendingAction": pending_action}
        confirmation_service.store_pending_action(state.get("session_id", ""), pending_action)
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


def _confirmed_pending_action_response(state: ChatGraphState, pending_action: dict[str, Any]) -> ChatGraphState:
    original_intent = pending_action.get("originalIntent") or pending_action.get("intent") or "confirmation"
    action_ref = pending_action.get("actionRef")
    confirmed_action = {
        **pending_action,
        "status": "CONFIRMED_SAFE_MODE",
        "realBackendMutationBlocked": True,
    }
    return {
        **state,
        "response": (
            "Confirmacion recibida. La accion quedo validada en modo seguro, "
            "pero no se ejecuto una compra real porque las mutaciones reales estan deshabilitadas."
        ),
        "state": ChatState.WAITING_CONFIRMATION.value,
        "intent": original_intent,
        "ui_action": UiActionType.NONE.value,
        "links": [],
        "next_step": "CONFIRMATION_RECEIVED_MUTATION_BLOCKED",
        "requires_confirmation": False,
        "action_ref": action_ref,
        "pending_action": None,
        "tool_result": None,
        "context": {**state.get("context", {}), "selectedBookId": pending_action.get("selectedBookId")},
        "metadata": {
            **state.get("metadata", {}),
            "detectedIntent": original_intent,
            "originalIntent": original_intent,
            "actionRef": action_ref,
            "confirmedAction": confirmed_action,
            "realBackendMutationBlocked": True,
        },
        "is_terminal": True,
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
    pending_clarification = None
    if next_action in {"ASK_BOOK_IDENTIFIER", "ASK_BOOK_AND_BRANCH", "ASK_BOOK_AND_QUANTITY"}:
        pending_clarification = _set_pending_clarification(
            state,
            intent=state.get("intent", "book_detail"),
            expected_entity="book",
        )
    _remember_last_intent(state, state.get("intent", "unknown"), next_action)
    return {
        **state,
        "response": response,
        "state": ChatState.ASKING_DETAILS.value,
        "next_step": next_action,
        "requires_confirmation": requires_confirmation,
        "metadata": {
            **state.get("metadata", {}),
            "pendingClarification": pending_clarification,
        },
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
    last_book = _get_last_book_from_memory(state) if original_intent == "purchase_intent" else None
    metadata = {
        **state.get("metadata", {}),
        "detectedIntent": "auth_required",
        "originalIntent": original_intent,
        "authRequired": True,
        "guest": True,
    }
    context = dict(state.get("context", {}))
    if isinstance(last_book, dict):
        if last_book.get("id"):
            context["selectedBookId"] = last_book["id"]
            metadata["selectedBookId"] = last_book["id"]
        if last_book.get("title"):
            metadata["bookTitle"] = last_book["title"]
    return {
        **state,
        "response": _auth_required_message(original_intent),
        "state": ChatState.NEEDS_CLARIFICATION.value,
        "intent": "auth_required",
        "ui_action": UiActionType.NONE.value,
        "links": AUTH_REQUIRED_SERVICE.build_auth_links(),
        "context": context,
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
        page_context=state.get("page_context"),
        source=state.get("source", "DOTNET_BACKEND"),
    )


def _is_guest_state(state: ChatGraphState) -> bool:
    return AUTH_REQUIRED_SERVICE.is_guest_context(
        state.get("roles", []),
        state.get("user_id"),
        state.get("permissions", []),
    )


def _detect_intent(normalized: str) -> str:
    if _is_identity_help_intent(normalized):
        return "identity_help"
    if _is_greeting_intent(normalized):
        return "greeting"
    if _contains_admin_inventory_adjustment(normalized):
        return "admin_inventory_adjustment"
    if _contains_admin_navigation_intent(normalized):
        return "admin_navigation"
    if _is_category_followup_request(normalized):
        return "refine_catalog_filter"
    if _is_stock_context_query(normalized):
        return "stock_context_query"
    if _is_stock_explicit_query(normalized):
        return "stock_explicit_query"
    if any(phrase in normalized for phrase in CONFIRM_SALE_PHRASES):
        return "confirm_sale"
    if any(keyword in normalized for keyword in ("factura", "recibo", "comprobante")):
        return "invoice_query"
    if any(phrase in normalized for phrase in CHECKOUT_CART_PHRASES):
        return "checkout_cart"
    if _contains_category_list_intent(normalized):
        return "list_categories"
    if any(phrase in normalized for phrase in ("solicitud de compra", "pedir proveedor", "comprar inventario")):
        return "purchase_request"
    if any(phrase in normalized for phrase in ("traslado", "mover sede", "transferir")):
        return "transfer_request"
    if any(phrase in normalized for phrase in ("registrar entrada", "entrada de inventario")):
        return "inventory_entry"

    if _is_summary_or_info_request(normalized):
        return "book_detail"

    has_purchase = _contains_purchase_intent(normalized)
    has_detail = _contains_book_detail_intent(normalized)
    has_stock = _contains_stock_check_intent(normalized)
    has_catalog = _contains_catalog_search_intent(normalized)

    if has_purchase:
        return "purchase_intent"
    if has_detail:
        return "book_detail"
    if has_stock:
        return "stock_check"
    if has_catalog:
        return "catalog_search"
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
        ("stock_check", ("stock", "disponible", "disponibilidad", "existencias", "quedan unidades")),
        (
            "confirm_sale",
            CONFIRM_SALE_PHRASES,
        ),
        (
            "checkout_cart",
            CHECKOUT_CART_PHRASES,
        ),
        (
            "purchase_intent",
            PURCHASE_INTENT_PHRASES,
        ),
        (
            "book_detail",
            (
                "detalle",
                "informacion del libro",
                "ver libro",
                "muestrame",
                *NATURAL_BOOK_DETAIL_PHRASES,
            ),
        ),
        ("catalog_search", ("buscar", "catalogo", "libro", "libros", "tienen", "recomienda", "recomiendame", "recomendame")),
        ("general_help", ("hola", "ayuda", "que puedes hacer")),
    ]
    for intent, keywords in priority_rules:
        if any(keyword in normalized for keyword in keywords):
            return intent
    return "unknown"


def _contains_book_detail_intent(normalized: str) -> bool:
    return any(phrase in normalized for phrase in NATURAL_BOOK_DETAIL_PHRASES)


def _is_followup_recommendation_request(normalized: str) -> bool:
    if not normalized:
        return False

    followup_phrases = (
        "algun otro",
        "alguna otra",
        "dame otro",
        "dame otra",
        "que mas me recomiendas",
        "que otro me recomiendas",
        "otro de esos",
        "otro libro",
        "otra recomendacion",
        "recomiendame otro",
        "recomendame otro",
        "siguiente libro",
        "uno mas",
    )
    if any(phrase in normalized for phrase in followup_phrases):
        return True

    return normalized.strip(" ?") in {"otro", "otra", "siguiente", "uno mas"}


def _contains_stock_check_intent(normalized: str) -> bool:
    if "libros" in normalized and not any(keyword in normalized for keyword in ("stock", "disponible", "disponibilidad")):
        return False

    return normalized.startswith("esta ") or any(phrase in normalized for phrase in STOCK_CHECK_PHRASES)


def _is_greeting_intent(normalized: str) -> bool:
    if not normalized:
        return False
    return normalized in GREETING_PHRASES or any(normalized.startswith(f"{phrase} ") for phrase in GREETING_PHRASES)


def _is_identity_help_intent(normalized: str) -> bool:
    if not normalized:
        return False
    return any(phrase in normalized for phrase in IDENTITY_HELP_PHRASES)


def _is_stock_context_query(normalized: str) -> bool:
    if not normalized:
        return False
    if any(phrase in normalized for phrase in STOCK_EXPLICIT_PHRASES):
        return False
    return any(phrase in normalized for phrase in STOCK_CONTEXT_PHRASES)


def _is_stock_explicit_query(normalized: str) -> bool:
    return "cuantos libros de" in normalized or "cuantos libros hay disponibles del libro" in normalized


def _is_contextual_purchase_intent(normalized: str, state: ChatGraphState) -> bool:
    if not normalized or not _get_last_book_from_memory(state):
        return False
    contextual_purchase_phrases = (
        "quiero uno",
        "quiero 1",
        "dame uno",
        "agregame uno",
        "agrega uno",
        "lo quiero",
        "quiero ese",
        "quiero ese libro",
        "quiero comprarlo",
        "lo compro",
        "me llevo uno",
    )
    return any(phrase in normalized for phrase in contextual_purchase_phrases)


def _is_category_followup_request(normalized: str) -> bool:
    if not normalized or _contains_category_list_intent(normalized):
        return False
    if _is_technical_catalog_query(normalized):
        return False

    category_filters = _catalog_filters(normalized)
    if not category_filters.get("genre"):
        return False

    category_patterns = (
        r"^de\s+.+$",
        r"^solo\s+(?:de\s+)?.+$",
        r"^pero\s+de\s+.+$",
        r"^(?:uno|una|algo|alguno|alguna)\s+(?:de\s+)?(?:categoria\s+)?[\w\s]+$",
        r"^un\s+libro\s+de\s+categoria\s+.+$",
        r"^libros\s+(?:de\s+)?categoria\s+.+$",
        r"^libros\s+.+$",
        r"^algo\s+de\s+.+$",
    )
    return any(re.search(pattern, normalized) for pattern in category_patterns)


def _contains_admin_inventory_adjustment(normalized: str) -> bool:
    if not normalized:
        return False
    has_inventory_word = any(word in normalized for word in ("stock", "inventario", "unidad", "unidades"))
    has_adjustment_word = any(phrase in normalized for phrase in ADMIN_INVENTORY_ADJUSTMENT_PHRASES)
    return has_inventory_word and has_adjustment_word


def _contains_admin_navigation_intent(normalized: str) -> bool:
    return any(phrase in normalized for phrase in ADMIN_NAVIGATION_PHRASES)


def _contains_catalog_search_intent(normalized: str) -> bool:
    return _is_technical_catalog_query(normalized) or any(phrase in normalized for phrase in CATALOG_SEARCH_PHRASES) or any(
        genre in normalized and "libro" in normalized for genre in GENRE_WORDS
    )


def _contains_refine_catalog_request(normalized: str, state: ChatGraphState) -> bool:
    if not normalized:
        return False

    if _contains_category_list_intent(normalized):
        return False

    last_catalog = _get_last_catalog_for_context(state)
    has_last_catalog = bool(last_catalog and isinstance(last_catalog.get("books"), list))
    category_ref = _extract_refined_catalog_filters(normalized, state)
    if category_ref:
        if "genre" in category_ref:
            return True
    if has_last_catalog and any(
        token in normalized for token in CATEGORY_LIST_WORDS | {"solo", "solo que", "solo de", "solo la", "solo los", "filtra", "filtrar", "filtro", "muestrame", "dame"}
    ):
        return True
    return False


def _contains_category_list_intent(normalized: str) -> bool:
    if _is_specific_book_category_question(normalized):
        return False

    if any(phrase in normalized for phrase in CATEGORY_LIST_PHRASES):
        return True

    category_markers = (
        "categorias",
        "categoria del catalogo",
        "categoria de catalogo",
        "categorias del catalogo",
        "categorias de catalogo",
        "categorias de libros",
        "categorias del libro",
        "generos",
        "tipos de libros",
    )
    return any(marker in normalized for marker in category_markers)


def _is_specific_book_category_question(normalized: str) -> bool:
    if not any(marker in normalized for marker in ("categoria tiene", "categoria es", "categoria del libro")):
        return False

    generic_markers = ("catalogo", "libros", "disponibles", "hay")
    if any(marker in normalized for marker in generic_markers):
        return False

    words = [word for word in normalized.split() if word not in {"que", "cual", "cuales", "categoria", "categorias", "tiene", "es", "del", "de", "libro", "el", "la", "un", "una"}]
    return bool(words)


def _contains_purchase_intent(normalized: str) -> bool:
    if any(phrase in normalized for phrase in CHECKOUT_CART_PHRASES):
        return False
    if _is_read_only_book_request(normalized):
        return False
    if _is_summary_or_info_request(normalized):
        return False

    return any(_contains_normalized_phrase(normalized, phrase) for phrase in PURCHASE_INTENT_PHRASES) or re.search(
        r"\bx\s*-?\d+\b",
        normalized,
    ) is not None


def _is_read_only_book_request(normalized: str) -> bool:
    protected_markers = (
        "comprar",
        "compra",
        "carrito",
        "llevar",
        "llevarme",
        "pagar",
        "finalizar",
        "confirmar",
        "factura",
        "venta",
    )
    if any(_contains_normalized_phrase(normalized, marker) for marker in protected_markers):
        return False

    read_only_markers = (
        "recomiendame",
        "recomendame",
        "recomienda",
        "recomendacion",
        "recomendaciones",
        "busca",
        "buscar",
        "busco",
        "catalogo",
        "categorias",
        "generos",
        "tipos de libros",
    )
    return (
        _contains_category_list_intent(normalized)
        or _contains_catalog_search_intent(normalized)
        or any(marker in normalized for marker in read_only_markers)
    )


def _contains_normalized_phrase(normalized: str, phrase: str) -> bool:
    pattern = r"\b" + r"\s+".join(re.escape(part) for part in phrase.split()) + r"\b"
    return re.search(pattern, normalized) is not None


def _is_summary_or_info_request(message: str) -> bool:
    normalized = _normalize(message)
    markers = (
        "resumen",
        "resumeme",
        "sinopsis",
        "descripcion",
        "de que trata",
        "informacion",
        "quien escribio",
        "precio",
        "cuanto cuesta",
    )
    return any(marker in normalized for marker in markers)


def _allowed_llm_intents(permission_service: PermissionService) -> list[str]:
    return [intent for intent in permission_service.INTENT_PERMISSIONS if intent != "unknown"]


def _extract_catalog_query(message: str) -> str | None:
    normalized = _normalize(message)
    stop_words = {
        "buscar",
        "busco",
        "busca",
        "buscame",
        "catalogo",
        "libro",
        "libros",
        "tienen",
        "tienes",
        "recomienda",
        "recomiendame",
        "recomendame",
        "recomendacion",
        "recomendaciones",
        "recomiendes",
        "recomendar",
        "muestrame",
        "quiero",
        "excelente",
        "que",
        "me",
        "ver",
        "para",
        "parecidos",
        "parecido",
        "como",
        "autor",
        "de",
        "del",
        "a",
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


def _extract_sale_id(message: str) -> str | None:
    match = re.search(
        r"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        message,
    )
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


def _purchase_can_default_to_one(message: str) -> bool:
    normalized = _normalize(message)
    if not _contains_purchase_intent(normalized):
        return False
    if re.search(r"\bbook-\d+\b", normalized):
        return False

    return not _has_explicit_quantity_reference(normalized)


def _has_explicit_quantity_reference(normalized: str) -> bool:
    if re.search(r"\bx\s*-?\d+\b", normalized) or re.search(r"(?<![\w-])(-?\d+)\b", normalized):
        return True

    quantity_words = {"un", "una", "uno", "dos", "tres", "cuatro", "cinco"}
    return any(re.search(rf"\b{word}\b", normalized) for word in quantity_words)


def _find_book_from_message(message: str, tool_service: BiblioBotToolService) -> dict | None:
    normalized = _normalize(message)
    candidate_queries = _extract_book_lookup_queries(message)

    for query in candidate_queries:
        books = tool_service.mock_client.search_books(query)
        book = _select_best_book_match(books, normalized, candidate_queries)

        if book:
            return tool_service.mock_client.get_book_detail(book["id"])

    books = tool_service.mock_client.search_books()
    book = _select_best_book_match(books, normalized, candidate_queries)

    if book:
        return tool_service.mock_client.get_book_detail(book["id"])

    return None


def _find_book_from_stock_message(message: str, tool_service: BiblioBotToolService) -> dict | None:
    book = _find_book_from_message(message, tool_service)
    if book:
        return book

    normalized = _normalize(message)
    candidate = re.sub(
        r"\b(?:cuantos\s+libros\s+hay\s+disponibles\s+del\s+libro|cuantos\s+libros\s+de|cuantos\s+hay\s+de|stock\s+de|disponibilidad\s+de|cuantos\s+quedan\s+de|cuantos\s+tienes\s+de|quiero\s+que\s+saques\s+uno\s+del\s+stock\s+(?:y\s+que\s+queden\s+\d+\s+libros\s+)?de|saques\s+uno\s+del\s+stock\s+de|saca\s+uno\s+del\s+stock\s+de|quita\s+uno\s+del\s+stock\s+de|descuenta\s+uno\s+del\s+inventario\s+de)\b",
        " ",
        normalized,
    )
    candidate = re.sub(r"\b(?:y\s+que\s+queden\s+\d+\s+libros?|para\s+que\s+queden\s+\d+\s+libros?)\b", " ", candidate)
    candidate = " ".join(word for word in candidate.split() if word not in {"el", "la", "los", "las", "un", "una", "de", "del"})
    if not candidate:
        return None

    books = tool_service.mock_client.search_books(candidate)
    selected = _select_best_book_match(books, normalized, [candidate])
    return tool_service.mock_client.get_book_detail(selected["id"]) if selected else None


def _extract_inventory_adjustment_details(message: str) -> dict[str, Any]:
    normalized = _normalize(message)
    adjustment_type = "OUT" if any(word in normalized for word in ("saca", "saques", "quita", "descuenta", "salida", "baja")) else "IN"
    expected_stock_after = None
    expected_match = re.search(r"\b(?:queden|quede|stock\s+a|inventario\s+a|a)\s+(\d+)\b", normalized)
    if expected_match:
        expected_stock_after = int(expected_match.group(1))

    quantity = None
    if re.search(r"\b(?:saca|saques|quita|descuenta|registra\s+salida\s+de)\s+(?:un|una|uno|1)\b", normalized):
        quantity = 1
    elif re.search(r"\b(?:agrega|sube)\s+(?:un|una|uno|1)\b", normalized):
        quantity = 1
    else:
        quantity = _extract_quantity(message)
        if quantity == expected_stock_after:
            quantity = None

    return {
        "adjustmentType": adjustment_type,
        "quantity": quantity,
        "expectedStockAfter": expected_stock_after,
    }


def _admin_navigation_target(normalized: str) -> dict[str, Any] | None:
    if "usuario" in normalized or "usuarios" in normalized:
        create = any(word in normalized for word in ("agregar", "crear", "registrar", "nuevo"))
        return {
            "target": "users",
            "route": FRONTEND_ACTION_SERVICE.ADMIN_USERS_ROUTE,
            "uiAction": UiActionType.NAVIGATE_TO_ADMIN_CREATE_USER.value if create else UiActionType.NAVIGATE_TO_ADMIN_USERS.value,
            "label": "Abrir usuarios",
            "linkType": "ADMIN_USERS",
            "permissions": ["admin.users.read", "admin.users.write", "admin.users.create"],
            "response": "Claro, te llevo al modulo de usuarios para continuar desde el panel.",
        }
    if "inventario" in normalized or "stock" in normalized:
        return {
            "target": "inventory",
            "route": FRONTEND_ACTION_SERVICE.ADMIN_INVENTORY_ROUTE,
            "uiAction": UiActionType.NAVIGATE_TO_ADMIN_INVENTORY.value,
            "label": "Abrir inventario",
            "linkType": "ADMIN_INVENTORY",
            "permissions": ["inventory.read", "inventory.entry", "inventory.adjust", "inventory.write"],
            "response": "Claro, te llevo al modulo de inventario.",
        }
    if "ventas" in normalized or "pedidos" in normalized:
        return {
            "target": "sales",
            "route": FRONTEND_ACTION_SERVICE.DASHBOARD_ROUTE,
            "uiAction": UiActionType.NAVIGATE_TO_ADMIN_SALES.value,
            "label": "Abrir dashboard",
            "linkType": "ADMIN_DASHBOARD",
            "permissions": ["sales.read_all", "sales.confirm"],
            "response": "Puedo orientarte desde el dashboard para revisar ventas.",
        }
    if "facturas" in normalized:
        return {
            "target": "invoices",
            "route": FRONTEND_ACTION_SERVICE.DASHBOARD_ROUTE,
            "uiAction": UiActionType.NAVIGATE_TO_ADMIN_INVOICES.value,
            "label": "Abrir dashboard",
            "linkType": "ADMIN_DASHBOARD",
            "permissions": ["invoices.read_all"],
            "response": "Te llevo al dashboard para revisar facturas disponibles.",
        }
    if "reportes" in normalized:
        return {
            "target": "reports",
            "route": FRONTEND_ACTION_SERVICE.DASHBOARD_ROUTE,
            "uiAction": UiActionType.NAVIGATE_TO_ADMIN_REPORTS.value,
            "label": "Abrir dashboard",
            "linkType": "ADMIN_DASHBOARD",
            "permissions": ["reports.read"],
            "response": "Te llevo al dashboard disponible para revisar reportes.",
        }
    if "solicitudes" in normalized:
        return {
            "target": "requests",
            "route": FRONTEND_ACTION_SERVICE.DASHBOARD_ROUTE,
            "uiAction": UiActionType.NAVIGATE_TO_ADMIN_REQUESTS.value,
            "label": "Abrir dashboard",
            "linkType": "ADMIN_DASHBOARD",
            "permissions": ["requests.transfer.create", "requests.purchase.create"],
            "response": "Te llevo al dashboard para revisar solicitudes internas.",
        }
    return None


def _can_access_admin_target(state: ChatGraphState, target: dict[str, Any]) -> bool:
    permissions = state.get("permissions", [])
    required = target.get("permissions") if isinstance(target, dict) else None
    if not isinstance(required, list) or not required:
        return False
    return PermissionService().has_any_permission(permissions, required)


def _extract_book_lookup_queries(message: str) -> list[str]:
    normalized = _normalize(message)
    candidates: list[str] = []

    intent_patterns = [
        r"^\s*y\s+",
        r"\b(?:dame\s+un\s+resumen\s+de\s+que\s+trata|dame\s+resumen\s+de\s+que\s+trata|dame\s+un\s+resumen\s+de|dame\s+resumen\s+de|resumen\s+de|sinopsis\s+de|descripcion\s+de|resumeme|me\s+puedes\s+resumir|cuentame\s+de\s+que\s+trata|quiero\s+saber\s+de\s+que\s+trata)\s+",
        r"\b(?:ver|mostrar|muestrame)\s+(?:el\s+|la\s+|un\s+|una\s+)?(?:libro|libros)\s+",
        r"\b(?:detalle|detalles|informacion)\s+(?:de|del)?\s*(?:el\s+|la\s+|un\s+|una\s+)?(?:libro|libros)?\s*",
        r"\b(?:dime\s+sobre|dime\s+de|dime\s+algo\s+de|dime\s+algo\s+sobre|hablame\s+de|hablame\s+sobre|hablo\s+de|hablo\s+del\s+libro|cuentame\s+de|cuentame\s+sobre|quiero\s+saber\s+de|quiero\s+saber\s+sobre|quiero\s+saber\s+acerca\s+de|que\s+sabes\s+de|informacion\s+de|informacion\s+sobre|info\s+de|info\s+sobre|de\s+que\s+trata|quien\s+escribio|precio\s+de|cuanto\s+cuesta)\s+",
        r"\b(?:hay\s+)?(?:stock|disponibilidad|disponible|existencias)\s+(?:de|del)?\s*(?:el\s+|la\s+|un\s+|una\s+)?",
        r"\b(?:tienes|tienen|hay|existe|esta\s+disponible|quedan\s+unidades\s+de|cuantos\s+hay\s+de)\s+(?:de|del)?\s*(?:el\s+|la\s+|un\s+|una\s+)?",
        r"\b(?:quiero\s+)?(?:comprar|compra|llevar|llevarme|agregame|agrega|agregar|anade|anadir|dame|pon|mete)\s+(?:\d+\s+)?(?:unidades?\s+de\s+|de\s+)?",
        r"\b(?:registrar|crear|solicitud)\s+(?:entrada|traslado|compra|de compra)\s+(?:de|del)?\s*",
    ]

    for pattern in intent_patterns:
        candidate = re.sub(pattern, " ", normalized).strip()
        candidate = _remove_trailing_book_noise(candidate)
        if candidate != normalized:
            _add_book_lookup_candidate(candidates, candidate)

    stop_words = {
        "ver",
        "y",
        "resumen",
        "sinopsis",
        "descripcion",
        "resumeme",
        "libro",
        "libros",
        "detalle",
        "detalles",
        "informacion",
        "dime",
        "algo",
        "sobre",
        "hablame",
        "cuentame",
        "acerca",
        "saber",
        "sabes",
        "que",
        "trata",
        "quien",
        "escribio",
        "precio",
        "cuanto",
        "cuesta",
        "muestrame",
        "mostrar",
        "abre",
        "interesa",
        "quiero",
        "como",
        "invitado",
        "de",
        "del",
        "al",
        "el",
        "la",
        "un",
        "una",
        "hay",
        "tienes",
        "tienen",
        "existe",
        "lo",
        "tienes",
        "tienen",
        "esta",
        "stock",
        "disponible",
        "disponibilidad",
        "existencias",
        "quedan",
        "unidades",
        "unidad",
        "cuantos",
        "comprar",
        "compra",
        "llevar",
        "llevarme",
        "agregame",
        "agrega",
        "agregar",
        "anade",
        "anadir",
        "dame",
        "pon",
        "mete",
        "carrito",
        "registrar",
        "entrada",
        "crear",
        "traslado",
        "solicitud",
        "para",
        "en",
        "desde",
        "hacia",
        "a",
        "sede",
        "norte",
        "sur",
        "centro",
        "central",
        "otro",
        "otra",
        "otros",
        "otras",
        "siguiente",
        "mas",
    }
    words = [
        word
        for word in normalized.split()
        if word not in stop_words and not re.fullmatch(r"x?-?\d+", word)
    ]
    _add_book_lookup_candidate(candidates, " ".join(words))
    _add_book_lookup_candidate(candidates, normalized)

    return candidates


def _has_explicit_book_query(message: str) -> bool:
    return any(not _is_contextual_book_query(query) for query in _extract_book_lookup_queries(message))


def _is_contextual_book_query(query: str) -> bool:
    normalized = _normalize(query)
    if not normalized:
        return True

    intent_noise_words = {
        "al",
        "de",
        "del",
        "dame",
        "el",
        "la",
        "libros",
        "que",
        "resumen",
        "sobre",
        "trata",
        "un",
        "una",
        "y",
    }
    contextual_words = {
        "anterior",
        "ese",
        "este",
        "libro",
        "primer",
        "primera",
        "primero",
        "segunda",
        "segundo",
        "tercera",
        "tercero",
        "cuarta",
        "cuarto",
        "quinta",
        "quinto",
    }
    meaningful_words = [word for word in normalized.split() if word not in intent_noise_words]
    return not meaningful_words or all(word in contextual_words for word in meaningful_words)


def _add_book_lookup_candidate(candidates: list[str], value: str | None) -> None:
    if not value:
        return

    candidate = " ".join(
        word
        for word in _normalize(value).split()
        if not re.fullmatch(r"x?-?\d+", word)
    )

    if len(candidate) < 2 or candidate in candidates:
        return

    candidates.append(candidate)


def _remove_trailing_book_noise(value: str) -> str:
    cleaned = value
    noise_patterns = [
        r"\b(?:lo\s+)?(?:tienes|tienen)\b",
        r"\b(?:esta\s+)?disponible\b",
        r"\bhay\b",
        r"\bstock\b",
        r"\ben\s+el\s+carrito\b",
        r"\bal\s+carrito\b",
        r"\bpor\s+favor\b",
    ]
    for pattern in noise_patterns:
        cleaned = re.sub(pattern, " ", cleaned)
    return " ".join(cleaned.split())


def _select_best_book_match(
    books: list[dict],
    normalized_message: str,
    candidate_queries: list[str],
) -> dict | None:
    valid_books = [book for book in books if isinstance(book, dict) and book.get("id") and book.get("title")]

    if not valid_books:
        return None

    for book in valid_books:
        if _normalize(str(book.get("id", ""))) in normalized_message:
            return book

    for book in valid_books:
        title = _normalize(str(book.get("title", "")))

        if title and title in normalized_message:
            return book

    for query in candidate_queries:
        for book in valid_books:
            title = _normalize(str(book.get("title", "")))

            if title == query or query in title or title in query:
                return book

    if len(valid_books) == 1 and _book_is_relevant(valid_books[0], normalized_message, candidate_queries):
        return valid_books[0]

    return None


def _book_is_relevant(book: dict, normalized_message: str, candidate_queries: list[str]) -> bool:
    title = _normalize(str(book.get("title", "")))
    author = _normalize(str(book.get("author", "")))
    genre = _normalize(str(book.get("genre", "")))
    authors = _normalize(_join_book_values(book.get("authors")))
    categories = _normalize(_join_book_values(book.get("categories")))
    searchable_text = " ".join([title, author, genre, authors, categories]).strip()

    if not searchable_text:
        return False

    if title and title in normalized_message:
        return True

    return any(query and (query in searchable_text or searchable_text in query) for query in candidate_queries)


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
    if _is_technical_catalog_query(normalized):
        return {"genre": "software"}
    for genre in sorted(GENRE_WORDS, key=len, reverse=True):
        normalized_genre = CATEGORY_SYNONYMS.get(genre, genre)
        if normalized_genre in normalized or genre in normalized:
            return {"genre": normalized_genre}
    return {"query": query}


def _catalog_search_query(query: str | None) -> str | None:
    if query and _is_technical_catalog_query(_normalize(query)):
        return None
    return query


def _is_technical_catalog_query(normalized: str) -> bool:
    return any(term in normalized for term in TECHNICAL_CATALOG_TERMS)


def _is_explicit_out_of_domain_request(normalized: str) -> bool:
    return any(phrase in normalized for phrase in EXPLICIT_OUT_OF_DOMAIN_PHRASES)


def _has_domain_signal(normalized: str) -> bool:
    if not normalized:
        return False
    if _is_explicit_out_of_domain_request(normalized):
        return False
    domain_terms = DOMAIN_SIGNAL_TERMS | GENRE_WORDS | TECHNICAL_CATALOG_TERMS | set(CATEGORY_LIST_WORDS)
    return any(term in normalized for term in domain_terms)


def _should_skip_llm_response_improvement(state: ChatGraphState) -> bool:
    intent = state.get("intent", "unknown")
    next_step = state.get("next_step")
    controlled_intents = {
        "admin_inventory_adjustment",
        "admin_navigation",
        "auth_required",
        "book_detail",
        "catalog_search",
        "checkout_cart",
        "confirm_sale",
        "general_help",
        "greeting",
        "identity_help",
        "list_categories",
        "out_of_domain",
        "page_navigation",
        "purchase_intent",
        "refine_catalog_filter",
        "stock_context_query",
        "stock_explicit_query",
        "stock_check",
    }
    controlled_steps = {
        "AUTH_REQUIRED",
        "AWAIT_EXPLICIT_CONFIRMATION",
        "CART_UPDATED",
        "CONFIRMATION_RECEIVED_MUTATION_BLOCKED",
        "NAVIGATE_TO_ADMIN_CREATE_USER",
        "NAVIGATE_TO_ADMIN_INVENTORY",
        "NAVIGATE_TO_ADMIN_USERS",
        "NAVIGATE_TO_INVENTORY_ADJUSTMENT",
        "OUT_OF_DOMAIN",
        "PERMISSION_DENIED",
    }
    return bool(
        intent in controlled_intents
        or intent in SENSITIVE_INTENTS
        or next_step in controlled_steps
        or state.get("is_terminal")
    )


def _extract_refined_catalog_filters(message: str, state: ChatGraphState) -> dict[str, str]:
    extracted = _catalog_filters(message)
    last_catalog = _get_last_catalog_for_context(state)
    if not extracted and isinstance(last_catalog, dict):
        filters = last_catalog.get("filters")
        if isinstance(filters, dict) and filters.get("genre"):
            extracted = {"genre": str(filters["genre"])}

    active_category = _get_page_context_active_category(state)
    if extracted.get("genre"):
        return extracted

    if active_category and any(
        token in message for token in ("actual", "categoria", "esta categoria", "filtra", "filtrar", "solo")
    ):
        return {"genre": active_category}

    if extracted.get("query"):
        return extracted

    return {}


def _get_page_context_active_category(state: ChatGraphState) -> str | None:
    page_context = state.get("page_context")
    if not isinstance(page_context, dict):
        return None

    active_category = page_context.get("activeCategory")
    if not active_category:
        return None

    normalized = _normalize(str(active_category))
    return CATEGORY_SYNONYMS.get(normalized, normalized) if normalized else None


def _filter_books_by_catalog_filters(
    books: list[dict],
    filters: dict[str, str] | None,
) -> list[dict]:
    if not isinstance(filters, dict) or not filters:
        return books

    genre_filter = (filters.get("genre") or "").strip()
    if not genre_filter:
        return books

    normalized_filter = _normalize(genre_filter)
    target = CATEGORY_SYNONYMS.get(normalized_filter, normalized_filter)
    filtered = []
    for book in books:
        if not isinstance(book, dict):
            continue

        book_genre = _normalize(str(book.get("genre") or ""))
        book_genres = _normalize(_join_book_values(book.get("categories")))
        searchable_text = _normalize(
            " ".join(
                str(value or "")
                for value in (
                    book.get("title"),
                    book.get("author"),
                    book.get("description"),
                    book_genre,
                    book_genres,
                )
            )
        )
        if target in {"software", "tecnologia"} and any(term in searchable_text for term in TECHNICAL_CATALOG_TARGETS):
            filtered.append(book)
            continue
        if target == "infantil" and ("nino" in book_genre or "ninos" in book_genre or "infantil" in book_genre):
            filtered.append(book)
            continue
        if target in {book_genre, book_genres} or target in book_genre or target in book_genres:
            filtered.append(book)
    return filtered


def _build_catalog_filters_from_message(message: str, state: ChatGraphState) -> tuple[str | None, dict[str, str]]:
    normalized = _normalize(message)
    explicit = _extract_refined_catalog_filters(normalized, state)
    base_query = _extract_catalog_query(message)
    if explicit.get("genre") and (not base_query or "categoria" in _normalize(base_query)):
        base_query = explicit["genre"]
    last_catalog = _get_last_catalog_for_context(state)
    if not base_query and isinstance(last_catalog, dict):
        base_query = last_catalog.get("query")
    return base_query, explicit


def _get_catalog_books_for_refinement(
    state: ChatGraphState,
    tool_service: BiblioBotToolService,
    context: ToolExecutionContext,
    query: str | None,
) -> list[dict]:
    last_catalog = _get_last_catalog_for_context(state)
    last_books = None
    if isinstance(last_catalog, dict):
        last_books = last_catalog.get("books")

    last_query = last_catalog.get("query") if isinstance(last_catalog, dict) else None
    if isinstance(last_books, list) and last_books and (
        len(last_books) == 1 or not query or _normalize(str(query)) == _normalize(str(last_query or ""))
    ):
        return last_books

    visible_books = _get_visible_books_from_page_context(state)
    if visible_books:
        return visible_books

    result = tool_service.search_books(SearchBooksInput(query=query), context)
    books = result.get("books", [])
    if not isinstance(books, list):
        return []
    return books


def _base_metadata(request: ChatProcessRequest, intent: str) -> dict[str, Any]:
    guest = AUTH_REQUIRED_SERVICE.is_guest_context(request.roles, request.userId, request.permissions)
    return {
        "sessionId": request.sessionId,
        "source": request.source,
        "roles": request.roles,
        "permissions": request.permissions,
        "detectedIntent": intent,
        "guest": guest,
        "pageContext": _safe_page_context(request.pageContext),
    }


def _summarize_books(books: list[dict], limit: int = 5) -> list[dict]:
    return [_summarize_book(book) for book in books[:limit] if isinstance(book, dict)]


def _clean_category_names(categories: list[Any], limit: int = 15) -> list[str]:
    cleaned: list[str] = []
    seen: set[str] = set()
    for category in categories:
        label = _format_category_label(category)
        if not label:
            continue
        key = _normalize(label)
        if not key or key in seen:
            continue
        seen.add(key)
        cleaned.append(label)
        if len(cleaned) >= limit:
            break
    return cleaned


def _format_category_label(category: Any) -> str:
    raw = " ".join(str(category or "").replace("_", " ").split())
    if not raw:
        return ""

    small_words = {"de", "del", "la", "las", "el", "los", "y", "e", "en", "para"}
    words = []
    for index, word in enumerate(raw.split()):
        lower = word.lower()
        if index > 0 and lower in small_words:
            words.append(lower)
        elif word.islower() or word.isupper() or word.istitle():
            words.append(lower.capitalize())
        else:
            words.append(word)
    return " ".join(words)


def _summarize_book(book: dict) -> dict:
    author = book.get("author") or _join_book_values(book.get("authors"))
    genre = book.get("genre") or _join_book_values(book.get("categories"))

    return {
        "id": book.get("id", ""),
        "title": book.get("title", ""),
        "author": author,
        "genre": genre,
        "price": book.get("price"),
        "available": bool(book.get("available", False)),
    }


def _join_book_values(value: Any) -> str:
    if isinstance(value, list):
        return ", ".join(str(item).strip() for item in value if str(item).strip())
    if value is None:
        return ""
    return str(value).strip()


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
        "checkout_cart": "No puedo finalizar el carrito porque tu usuario no tiene el permiso necesario.",
        "confirm_sale": "No puedo confirmar esa venta porque tu usuario no tiene el permiso necesario.",
        "invoice_query": "No puedo mostrar esa factura porque tu usuario no tiene el permiso necesario.",
        "sales_query": "No puedo mostrar esa informacion porque tu usuario no tiene el permiso necesario.",
        "inventory_entry": "No puedo preparar esa entrada de inventario porque tu usuario no tiene el permiso necesario.",
        "admin_inventory_adjustment": "No puedo preparar ese ajuste de inventario porque tu usuario no tiene el permiso necesario.",
        "admin_navigation": "No puedo abrir ese modulo administrativo porque tu usuario no tiene el permiso necesario.",
        "transfer_request": "No puedo preparar esa solicitud de traslado porque tu usuario no tiene el permiso necesario.",
        "purchase_request": "No puedo preparar esa solicitud de compra porque tu usuario no tiene el permiso necesario.",
    }
    return messages.get(intent, "No puedo realizar esa accion porque tu usuario no tiene el permiso necesario.")


def _auth_required_message(intent: str) -> str:
    if intent in {"purchase_intent", "checkout_cart", "confirm_sale", "cart_manage", "cart_read", "create_sale"}:
        return (
            "Puedo ayudarte a encontrar el libro, pero para comprar o usar el carrito "
            "necesitas iniciar sesion o crear una cuenta."
        )
    if intent in {"admin_inventory_adjustment", "admin_navigation"}:
        return (
            "Para usar acciones administrativas necesitas iniciar sesion con una cuenta autorizada."
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


def _safe_page_context(page_context: Any) -> dict[str, Any] | None:
    raw_context = _plain_mapping(page_context)
    if not raw_context:
        return None

    safe_context: dict[str, Any] = {}
    for key in ("route", "pageTitle", "searchQuery", "activeCategory"):
        value = _safe_text(raw_context.get(key), max_length=180)
        if value:
            safe_context[key] = value

    active_filters = raw_context.get("activeFilters")
    if isinstance(active_filters, dict):
        safe_filters: dict[str, str] = {}
        for key, value in list(active_filters.items())[:8]:
            safe_key = _safe_text(key, max_length=48)
            safe_value = _safe_text(value, max_length=120)
            if safe_key and safe_value:
                safe_filters[safe_key] = safe_value
        if safe_filters:
            safe_context["activeFilters"] = safe_filters

    visible_books = raw_context.get("visibleBooks")
    if isinstance(visible_books, list):
        safe_books = [_safe_context_book(book) for book in visible_books[:10]]
        safe_books = [book for book in safe_books if book]
        if safe_books:
            safe_context["visibleBooks"] = safe_books

    selected_book = _safe_context_book(raw_context.get("selectedBook"))
    if selected_book:
        safe_context["selectedBook"] = selected_book

    cart_summary = _safe_cart_summary(raw_context.get("cartSummary"))
    if cart_summary:
        safe_context["cartSummary"] = cart_summary

    return safe_context or None


def _plain_mapping(value: Any) -> dict[str, Any]:
    if value is None:
        return {}
    if isinstance(value, dict):
        return value
    if hasattr(value, "model_dump"):
        dumped = value.model_dump(exclude_none=True)
        return dumped if isinstance(dumped, dict) else {}
    if hasattr(value, "dict"):
        dumped = value.dict(exclude_none=True)
        return dumped if isinstance(dumped, dict) else {}
    return {}


def _safe_context_book(value: Any) -> dict[str, Any] | None:
    raw_book = _plain_mapping(value)
    if not raw_book:
        return None

    book_id = _safe_text(raw_book.get("id"), max_length=120)
    title = _safe_text(raw_book.get("title"), max_length=180)
    if not book_id and not title:
        return None

    authors = _safe_text_list(raw_book.get("authors"), max_items=4, max_length=120)
    if not authors and raw_book.get("author"):
        author = _safe_text(raw_book.get("author"), max_length=120)
        authors = [author] if author else []

    categories = _safe_text_list(raw_book.get("categories"), max_items=4, max_length=80)
    if not categories and raw_book.get("genre"):
        genre = _safe_text(raw_book.get("genre"), max_length=80)
        categories = [genre] if genre else []

    safe_book: dict[str, Any] = {}
    if book_id:
        safe_book["id"] = book_id
    if title:
        safe_book["title"] = title
    if authors:
        safe_book["authors"] = authors
    if categories:
        safe_book["categories"] = categories

    price = _safe_number(raw_book.get("price"))
    if price is not None:
        safe_book["price"] = price

    if isinstance(raw_book.get("available"), bool):
        safe_book["available"] = raw_book["available"]

    return safe_book


def _safe_cart_summary(value: Any) -> dict[str, Any] | None:
    raw_summary = _plain_mapping(value)
    if not raw_summary:
        return None

    safe_summary: dict[str, Any] = {}
    for key in ("itemCount", "totalItems", "subtotal"):
        number = _safe_number(raw_summary.get(key))
        if number is not None:
            safe_summary[key] = number
    return safe_summary or None


def _safe_text_list(value: Any, max_items: int, max_length: int) -> list[str]:
    if not isinstance(value, list):
        return []
    safe_values: list[str] = []
    for item in value[:max_items]:
        text = _safe_text(item, max_length=max_length)
        if text:
            safe_values.append(text)
    return safe_values


def _safe_text(value: Any, max_length: int) -> str | None:
    if value is None:
        return None
    text = " ".join(str(value).split())
    if not text:
        return None
    return text[:max_length]


def _safe_number(value: Any) -> int | float | None:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        return None
    return value if value >= 0 else None


def _normalize(value: str) -> str:
    without_accents = "".join(
        char
        for char in unicodedata.normalize("NFD", value.lower())
        if unicodedata.category(char) != "Mn"
    )
    without_punctuation = re.sub(r"[¿?¡!,.;:()\[\]{}\"']", " ", without_accents)
    corrected_words = [
        COMMON_QUERY_CORRECTIONS.get(word, word)
        for word in without_punctuation.split()
    ]
    return " ".join(corrected_words)
