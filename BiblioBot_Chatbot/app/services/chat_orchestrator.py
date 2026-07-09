import re
import unicodedata

from app.clients import DotNetClientProtocol, get_dotnet_client
from app.schemas.chat_contract import (
    ChatContext,
    ChatLink,
    ChatProcessRequest,
    ChatProcessResponse,
    ChatState,
    UiActionType,
)
from app.services.confirmation_service import ConfirmationService
from app.services.llm_assistant_service import LlmAssistantService
from app.services.permission_service import PermissionService


class ChatOrchestratorService:
    def __init__(
        self,
        mock_client: DotNetClientProtocol | None = None,
        permission_service: PermissionService | None = None,
        confirmation_service: ConfirmationService | None = None,
        llm_assistant_service: LlmAssistantService | None = None,
    ):
        self.mock_client = mock_client or get_dotnet_client()
        self.permission_service = permission_service or PermissionService()
        self.confirmation_service = confirmation_service or ConfirmationService()
        self.llm_assistant_service = llm_assistant_service or LlmAssistantService()

    def process(self, request: ChatProcessRequest) -> ChatProcessResponse:
        try:
            from app.graph.chat_graph_service import ChatGraphService

            return ChatGraphService(
                mock_client=self.mock_client,
                permission_service=self.permission_service,
                confirmation_service=self.confirmation_service,
                llm_assistant_service=self.llm_assistant_service,
            ).process(request)
        except Exception:
            return self._legacy_process(request)

    def _legacy_process(self, request: ChatProcessRequest) -> ChatProcessResponse:
        if not request.sessionId.strip():
            return self._build_response(
                request=request,
                response="Falta la trazabilidad de sesion. Envia un sessionId valido para continuar.",
                state=ChatState.NEEDS_CLARIFICATION,
                intent="missing_session",
                next_action="REQUEST_VALID_SESSION",
            )

        if "chat.message" not in request.permissions:
            return self._build_response(
                request=request,
                response="No tienes permisos para usar el chat del sistema.",
                state=ChatState.FAILED,
                intent="permission_denied",
                next_action="PERMISSION_DENIED",
            )

        if not request.roles:
            return self._build_response(
                request=request,
                response="No se pudo identificar el rol del usuario. Verifica tu sesion e intenta nuevamente.",
                state=ChatState.FAILED,
                intent="missing_roles",
                next_action="REQUEST_VALID_ROLE",
            )

        if self.confirmation_service.is_explicit_cancellation(request.message):
            return self._build_response(
                request=request,
                response="No se ejecuto ninguna accion. Puedes continuar cuando quieras.",
                state=ChatState.IDLE,
                intent="cancel_confirmation",
                next_action="WAITING_USER_MESSAGE",
            )

        if self.confirmation_service.is_explicit_confirmation(request.message):
            return self._build_response(
                request=request,
                response="No hay una accion pendiente valida para confirmar. Indica primero que necesitas hacer.",
                state=ChatState.NEEDS_CLARIFICATION,
                intent="confirmation_without_pending_action",
                next_action="ASK_ACTION_DETAILS",
            )

        intent = self._detect_intent(request.message)
        if intent == "unknown":
            llm_intent = self.llm_assistant_service.suggest_intent(
                request.message,
                self._allowed_llm_intents(),
            )
            if llm_intent:
                intent = llm_intent

        if not self.permission_service.can_access_intent(intent, request.permissions):
            return self._permission_denied_response(request, intent)

        return self._response_for_intent(request, intent)

    def _response_for_intent(
        self,
        request: ChatProcessRequest,
        intent: str,
    ) -> ChatProcessResponse:
        permissions = set(request.permissions)

        if intent == "catalog_search":
            query = self._extract_catalog_query(request.message)
            books = self.mock_client.search_books(query)
            titles = [book["title"] for book in books[:3]]
            response = (
                "Encontre estos libros en el catalogo simulado: "
                + "; ".join(titles)
                + "."
                if titles
                else "No encontre coincidencias en el catalogo simulado. Prueba con titulo, autor o categoria."
            )
            return self._build_response(
                request=request,
                response=response,
                state=ChatState.INTENT_DETECTED,
                intent=intent,
                next_action="SEARCH_BOOKS_PENDING",
                ui_action=UiActionType.NAVIGATE_TO_CATALOG,
                metadata_extra={
                    "query": query,
                    "resultCount": len(books),
                    "books": self._summarize_books(books),
                },
            )

        if intent == "book_detail":
            book = self._find_book_from_message(request.message)
            if book:
                return self._build_response(
                    request=request,
                    response=(
                        f"{book['title']} de {book['author']}. "
                        f"Genero: {book['genre']}. Precio simulado: {book['price']}."
                    ),
                    state=ChatState.INTENT_DETECTED,
                    intent=intent,
                    next_action="BOOK_DETAIL_READY",
                    ui_action=UiActionType.NAVIGATE_TO_PRODUCT,
                    metadata_extra={"book": self._summarize_book(book)},
                )
            return self._build_response(
                request=request,
                response="Indica el nombre o identificador del libro para revisar su detalle.",
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_BOOK_IDENTIFIER",
            )

        if intent == "stock_check":
            book = self._find_book_from_message(request.message)
            if book:
                stock = self.mock_client.check_stock(book["id"])
                return self._build_response(
                    request=request,
                    response=(
                        f"Stock simulado para {book['title']}: "
                        f"{stock['totalStock']} unidades en total."
                    ),
                    state=ChatState.INTENT_DETECTED,
                    intent=intent,
                    next_action="STOCK_CHECK_READY",
                    metadata_extra={"stock": stock},
                )
            return self._build_response(
                request=request,
                response="Indica el libro y la sede o sucursal para revisar disponibilidad.",
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_BOOK_AND_BRANCH",
            )

        if intent == "purchase_intent":
            book = self._find_book_from_message(request.message)
            quantity = self._extract_quantity(request.message)
            if book and quantity:
                summary = f"Preparar compra de {quantity} unidad(es) de {book['title']}"
                action_ref = self.confirmation_service.build_action_ref(
                    request.sessionId,
                    intent,
                    summary,
                )
                pending_action = self.confirmation_service.build_pending_action(
                    intent=intent,
                    action_ref=action_ref,
                    summary=summary,
                    details={
                        "bookId": book["id"],
                        "title": book["title"],
                        "quantity": quantity,
                    },
                )
                return self._build_response(
                    request=request,
                    response=(
                        f"{summary}. Confirma explicitamente para continuar. "
                        "En esta fase no se confirma ninguna venta real."
                    ),
                    state=ChatState.WAITING_CONFIRMATION,
                    intent=intent,
                    next_action="AWAIT_EXPLICIT_CONFIRMATION",
                    requires_confirmation=True,
                    action_ref=action_ref,
                    metadata_extra={"pendingAction": pending_action},
                )
            return self._build_response(
                request=request,
                response="Indica el libro y la cantidad. No se hara ninguna compra sin confirmacion explicita.",
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_BOOK_AND_QUANTITY",
                requires_confirmation=True,
            )

        if intent == "invoice_query":
            invoice_id = self._extract_invoice_id(request.message)
            if invoice_id:
                invoice = self.mock_client.get_invoice(invoice_id)
                if invoice:
                    return self._build_response(
                        request=request,
                        response=(
                            f"Factura simulada {invoice['id']} encontrada. "
                            f"Total: {invoice['total']}. Estado: {invoice['status']}."
                        ),
                        state=ChatState.INTENT_DETECTED,
                        intent=intent,
                        next_action="INVOICE_READY",
                        ui_action=UiActionType.SHOW_INVOICE,
                        links=[
                            ChatLink(
                                label=f"Ver factura {invoice['id']}",
                                url=f"/facturas/{invoice['id']}",
                                type="invoice",
                            )
                        ],
                        metadata_extra={"invoice": invoice},
                    )
                return self._build_response(
                    request=request,
                    response="No encontre esa factura en los datos simulados.",
                    state=ChatState.NEEDS_CLARIFICATION,
                    intent=intent,
                    next_action="ASK_INVOICE_OR_SALE_ID",
                    metadata_extra={"invoiceId": invoice_id, "invoice": None},
                )
            return self._build_response(
                request=request,
                response="Indica el numero de factura o identificador de venta que quieres consultar.",
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_INVOICE_OR_SALE_ID",
            )

        if intent == "inventory_entry":
            if "inventory.entry" not in permissions:
                return self._build_response(
                    request=request,
                    response="No tienes permisos para registrar entradas de inventario.",
                    state=ChatState.FAILED,
                    intent=intent,
                    next_action="PERMISSION_DENIED",
                )
            return self._build_response(
                request=request,
                response=(
                    "Indica libro, cantidad, sede y motivo de la entrada. "
                    "No se registrara nada sin confirmacion explicita."
                ),
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_INVENTORY_ENTRY_DETAILS",
                requires_confirmation=True,
                metadata_extra={
                    "pendingAction": {
                        "intent": intent,
                        "status": "AWAITING_REQUIRED_DETAILS",
                        "mockOnly": True,
                    }
                },
            )

        if intent == "transfer_request":
            if "requests.transfer.create" not in permissions:
                return self._build_response(
                    request=request,
                    response="No tienes permisos para crear solicitudes de traslado.",
                    state=ChatState.FAILED,
                    intent=intent,
                    next_action="PERMISSION_DENIED",
                )
            return self._build_response(
                request=request,
                response=(
                    "Indica libro, cantidad, sede origen y sede destino. "
                    "No se creara nada sin confirmacion explicita."
                ),
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_TRANSFER_DETAILS",
                requires_confirmation=True,
                metadata_extra={
                    "pendingAction": {
                        "intent": intent,
                        "status": "AWAITING_REQUIRED_DETAILS",
                        "mockOnly": True,
                    }
                },
            )

        if intent == "purchase_request":
            if "requests.purchase.create" not in permissions:
                return self._build_response(
                    request=request,
                    response="No tienes permisos para crear solicitudes de compra interna.",
                    state=ChatState.FAILED,
                    intent=intent,
                    next_action="PERMISSION_DENIED",
                )
            return self._build_response(
                request=request,
                response=(
                    "Indica libro, cantidad y justificacion de la solicitud. "
                    "No se creara nada sin confirmacion explicita."
                ),
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_PURCHASE_REQUEST_DETAILS",
                requires_confirmation=True,
                metadata_extra={
                    "pendingAction": {
                        "intent": intent,
                        "status": "AWAITING_REQUIRED_DETAILS",
                        "mockOnly": True,
                    }
                },
            )

        if intent == "sales_query":
            if "sales.read_all" in permissions:
                sales = self.mock_client.query_sales("all")
                return self._build_response(
                    request=request,
                    response=f"Encontre {len(sales)} venta simulada para consulta general.",
                    state=ChatState.INTENT_DETECTED,
                    intent=intent,
                    next_action="QUERY_SALES_PENDING",
                    metadata_extra={"scope": "all", "sales": sales, "resultCount": len(sales)},
                )
            if "sales.read_own" in permissions:
                sales = self.mock_client.query_sales("own")
                return self._build_response(
                    request=request,
                    response=f"Encontre {len(sales)} venta simulada asociada a tu consulta.",
                    state=ChatState.INTENT_DETECTED,
                    intent=intent,
                    next_action="QUERY_OWN_SALES_PENDING",
                    metadata_extra={"scope": "own", "sales": sales, "resultCount": len(sales)},
                )
            return self._build_response(
                request=request,
                response="No tienes permisos para consultar ventas.",
                state=ChatState.FAILED,
                intent=intent,
                next_action="PERMISSION_DENIED",
            )

        if intent == "general_help":
            return self._build_response(
                request=request,
                response=(
                    "Puedo ayudarte con las funciones habilitadas por tus permisos: "
                    f"{self._describe_allowed_capabilities(request.permissions)}."
                ),
                state=ChatState.IDLE,
                intent=intent,
                next_action="WAITING_USER_MESSAGE",
            )

        return self._build_response(
            request=request,
            response=(
                "Puedo ayudarte con catalogo, disponibilidad, compras, facturas e inventario. "
                "Que necesitas hacer?"
            ),
            state=ChatState.NEEDS_CLARIFICATION,
            intent="unknown",
            next_action="ASK_CLARIFICATION",
        )

    def _detect_intent(self, message: str) -> str:
        normalized = self._normalize(message)

        priority_rules = [
            ("invoice_query", ("factura", "recibo", "comprobante")),
            ("purchase_request", ("solicitud de compra", "pedir proveedor", "comprar inventario")),
            ("sales_query", ("ventas", "venta de hoy", "reporte de ventas")),
            ("transfer_request", ("traslado", "mover sede", "transferir")),
            ("inventory_entry", ("inventario", "entrada", "registrar entrada")),
            ("stock_check", ("stock", "disponible", "disponibilidad", "existencias")),
            ("purchase_intent", ("comprar", "quiero llevar", "agregar al carrito", "anadir al carrito")),
            ("book_detail", ("detalle", "informacion del libro", "ver libro")),
            (
                "catalog_search",
                ("buscar", "catalogo", "libro", "tienen", "recomienda", "recomiendame", "recomendame"),
            ),
            ("general_help", ("hola", "ayuda", "que puedes hacer")),
        ]

        for intent, keywords in priority_rules:
            if any(keyword in normalized for keyword in keywords):
                return intent

        return "unknown"

    def _build_response(
        self,
        request: ChatProcessRequest,
        response: str,
        state: ChatState,
        intent: str,
        next_action: str,
        ui_action: UiActionType = UiActionType.NONE,
        requires_confirmation: bool = False,
        action_ref: str | None = None,
        links: list[ChatLink] | None = None,
        metadata_extra: dict | None = None,
    ) -> ChatProcessResponse:
        metadata = {
            "sessionId": request.sessionId,
            "source": request.source,
            "roles": request.roles,
            "permissions": request.permissions,
            "detectedIntent": intent,
        }
        if metadata_extra:
            metadata.update(metadata_extra)

        safe_response = self.llm_assistant_service.improve_response(response, request.message, intent)

        return ChatProcessResponse(
            response=safe_response,
            state=state,
            links=links or [],
            uiAction=ui_action,
            context=ChatContext(
                intent=intent,
                requiresConfirmation=requires_confirmation,
                actionRef=action_ref,
                saleOrigin="CHATBOT",
                nextAction=next_action,
                metadata=metadata,
            ),
        )

    def _allowed_llm_intents(self) -> list[str]:
        return [
            intent
            for intent in self.permission_service.INTENT_PERMISSIONS
            if intent not in {"unknown"}
        ]

    def _permission_denied_response(
        self,
        request: ChatProcessRequest,
        intent: str,
    ) -> ChatProcessResponse:
        required = self.permission_service.required_permissions_for_intent(intent)
        return self._build_response(
            request=request,
            response=self._permission_denied_message(intent),
            state=ChatState.FAILED,
            intent=intent,
            next_action="PERMISSION_DENIED",
            metadata_extra={"requiredPermissions": required},
        )

    def _permission_denied_message(self, intent: str) -> str:
        messages = {
            "catalog_search": "No tienes permisos para consultar el catalogo.",
            "book_detail": "No tienes permisos para consultar detalles de libros.",
            "stock_check": "No tienes permisos para consultar disponibilidad o inventario.",
            "purchase_intent": "No tienes permisos para iniciar compras por chat.",
            "invoice_query": "No tienes permisos para consultar facturas.",
            "sales_query": "No tienes permisos para consultar ventas.",
            "inventory_entry": "No tienes permisos para registrar entradas de inventario.",
            "transfer_request": "No tienes permisos para crear solicitudes de traslado.",
            "purchase_request": "No tienes permisos para crear solicitudes de compra interna.",
        }
        return messages.get(intent, "No tienes permisos para realizar esta accion.")

    def _extract_catalog_query(self, message: str) -> str | None:
        normalized = self._normalize(message)
        stop_words = {
            "buscar",
            "busco",
            "catalogo",
            "libro",
            "libros",
            "tienen",
            "recomienda",
            "recomiendame",
            "recomendame",
            "quiero",
            "de",
            "del",
            "la",
            "el",
            "un",
            "una",
        }
        words = [word for word in normalized.split() if word not in stop_words]
        return " ".join(words) or None

    def _extract_invoice_id(self, message: str) -> str | None:
        match = re.search(r"\bFAC-\d{4,}\b", message.upper())
        return match.group(0) if match else None

    def _extract_quantity(self, message: str) -> int | None:
        match = re.search(r"\b(\d+)\b", message)
        if match:
            return int(match.group(1))

        normalized = self._normalize(message)
        quantity_words = {
            "un": 1,
            "una": 1,
            "uno": 1,
            "dos": 2,
            "tres": 3,
            "cuatro": 4,
            "cinco": 5,
        }
        for word, quantity in quantity_words.items():
            if re.search(rf"\b{word}\b", normalized):
                return quantity
        return None

    def _find_book_from_message(self, message: str) -> dict | None:
        normalized = self._normalize(message)
        for book in self.mock_client.search_books():
            if self._normalize(book["id"]) in normalized or self._normalize(book["title"]) in normalized:
                return self.mock_client.get_book_detail(book["id"])
        return None

    def _summarize_books(self, books: list[dict]) -> list[dict]:
        return [self._summarize_book(book) for book in books[:5]]

    def _summarize_book(self, book: dict) -> dict:
        return {
            "id": book["id"],
            "title": book["title"],
            "author": book["author"],
            "genre": book["genre"],
            "price": book["price"],
            "available": book["available"],
        }

    def _describe_allowed_capabilities(self, permissions: list[str]) -> str:
        capabilities = []
        if self.permission_service.has_any_permission(permissions, ["books.read", "books.search"]):
            capabilities.append("catalogo y disponibilidad")
        if self.permission_service.has_any_permission(permissions, ["cart.manage", "sales.create"]):
            capabilities.append("preparacion de compras con confirmacion")
        if self.permission_service.has_any_permission(permissions, ["invoices.read_own", "invoices.read_all"]):
            capabilities.append("consulta de facturas")
        if self.permission_service.has_any_permission(permissions, ["sales.read_own", "sales.read_all"]):
            capabilities.append("consulta de ventas")
        if self.permission_service.has_any_permission(permissions, ["inventory.entry", "inventory.read"]):
            capabilities.append("inventario")
        if self.permission_service.has_any_permission(
            permissions,
            ["requests.transfer.create", "requests.purchase.create"],
        ):
            capabilities.append("solicitudes internas")
        return ", ".join(capabilities) if capabilities else "ayuda general del chat"

    def _normalize(self, value: str) -> str:
        without_accents = "".join(
            char
            for char in unicodedata.normalize("NFD", value.lower())
            if unicodedata.category(char) != "Mn"
        )
        return " ".join(without_accents.split())
