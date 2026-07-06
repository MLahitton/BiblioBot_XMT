import unicodedata

from app.schemas.chat_contract import (
    ChatContext,
    ChatProcessRequest,
    ChatProcessResponse,
    ChatState,
    UiActionType,
)


class ChatOrchestratorService:
    def process(self, request: ChatProcessRequest) -> ChatProcessResponse:
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

        intent = self._detect_intent(request.message)
        return self._response_for_intent(request, intent)

    def _response_for_intent(
        self,
        request: ChatProcessRequest,
        intent: str,
    ) -> ChatProcessResponse:
        permissions = set(request.permissions)

        if intent == "catalog_search":
            return self._build_response(
                request=request,
                response="Puedo ayudarte a buscar libros en el catalogo. Indica titulo, autor, categoria o una palabra clave.",
                state=ChatState.INTENT_DETECTED,
                intent=intent,
                next_action="SEARCH_BOOKS_PENDING",
                ui_action=UiActionType.NAVIGATE_TO_CATALOG,
            )

        if intent == "book_detail":
            return self._build_response(
                request=request,
                response="Indica el nombre o identificador del libro para revisar su detalle.",
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_BOOK_IDENTIFIER",
            )

        if intent == "stock_check":
            return self._build_response(
                request=request,
                response="Indica el libro y la sede o sucursal para revisar disponibilidad.",
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_BOOK_AND_BRANCH",
            )

        if intent == "purchase_intent":
            return self._build_response(
                request=request,
                response="Indica el libro y la cantidad. La compra no se confirmara todavia.",
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_BOOK_AND_QUANTITY",
                requires_confirmation=False,
            )

        if intent == "invoice_query":
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
                    "No se registrara nada sin confirmacion."
                ),
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_INVENTORY_ENTRY_DETAILS",
                requires_confirmation=False,
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
                    "No se creara nada sin confirmacion."
                ),
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_TRANSFER_DETAILS",
                requires_confirmation=False,
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
                    "No se creara nada sin confirmacion."
                ),
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_PURCHASE_REQUEST_DETAILS",
                requires_confirmation=False,
            )

        if intent == "sales_query":
            if "sales.read_all" in permissions:
                return self._build_response(
                    request=request,
                    response="Puedes consultar ventas. En esta fase aun no se consultan datos reales.",
                    state=ChatState.INTENT_DETECTED,
                    intent=intent,
                    next_action="QUERY_SALES_PENDING",
                )
            if "sales.read_own" in permissions:
                return self._build_response(
                    request=request,
                    response=(
                        "Puedes consultar tus propias ventas o compras. "
                        "En esta fase aun no se consultan datos reales."
                    ),
                    state=ChatState.INTENT_DETECTED,
                    intent=intent,
                    next_action="QUERY_OWN_SALES_PENDING",
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
                    "Puedo ayudarte con catalogo, disponibilidad, compras, facturas "
                    "y operaciones internas segun tus permisos."
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
                ("buscar", "catalogo", "libro", "tienen", "recomienda", "recomendame"),
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
    ) -> ChatProcessResponse:
        return ChatProcessResponse(
            response=response,
            state=state,
            links=[],
            uiAction=ui_action,
            context=ChatContext(
                intent=intent,
                requiresConfirmation=requires_confirmation,
                saleOrigin="CHATBOT",
                nextAction=next_action,
                metadata={
                    "sessionId": request.sessionId,
                    "source": request.source,
                    "roles": request.roles,
                    "permissions": request.permissions,
                    "detectedIntent": intent,
                },
            ),
        )

    def _normalize(self, value: str) -> str:
        without_accents = "".join(
            char
            for char in unicodedata.normalize("NFD", value.lower())
            if unicodedata.category(char) != "Mn"
        )
        return " ".join(without_accents.split())
