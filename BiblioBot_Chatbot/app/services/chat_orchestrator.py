import re
import unicodedata

from app.clients import MockDotNetClient
from app.schemas.chat_contract import (
    ChatContext,
    ChatLink,
    ChatProcessRequest,
    ChatProcessResponse,
    ChatState,
    UiActionType,
)


class ChatOrchestratorService:
    def __init__(self, mock_client: MockDotNetClient | None = None):
        self.mock_client = mock_client or MockDotNetClient()

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
            return self._build_response(
                request=request,
                response="Indica el libro y la cantidad. La compra no se confirmara todavia.",
                state=ChatState.ASKING_DETAILS,
                intent=intent,
                next_action="ASK_BOOK_AND_QUANTITY",
                requires_confirmation=False,
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
                requires_confirmation=False,
                metadata_extra={"mockPreparation": "inventory_entry"},
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
                requires_confirmation=False,
                metadata_extra={"mockPreparation": "transfer_request"},
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
                requires_confirmation=False,
                metadata_extra={"mockPreparation": "purchase_request"},
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

        return ChatProcessResponse(
            response=response,
            state=state,
            links=links or [],
            uiAction=ui_action,
            context=ChatContext(
                intent=intent,
                requiresConfirmation=requires_confirmation,
                saleOrigin="CHATBOT",
                nextAction=next_action,
                metadata=metadata,
            ),
        )

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

    def _normalize(self, value: str) -> str:
        without_accents = "".join(
            char
            for char in unicodedata.normalize("NFD", value.lower())
            if unicodedata.category(char) != "Mn"
        )
        return " ".join(without_accents.split())
