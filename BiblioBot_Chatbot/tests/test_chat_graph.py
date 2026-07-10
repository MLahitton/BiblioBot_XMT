from pathlib import Path

from fastapi.testclient import TestClient

from app.graph import ChatGraphService, build_chat_graph
from app.graph.nodes import (
    _catalog_result_state,
    _detect_intent,
    _extract_book_lookup_queries,
    _find_book_from_message,
    _normalize,
    final_safety_node,
)
from app.main import app
from app.schemas.chat_contract import ChatLink, ChatProcessRequest, ChatProcessResponse
from app.services import ConfirmationService, LlmAssistantService, PermissionService
from app.tools import BiblioBotToolService, get_langchain_tools


client = TestClient(app)


class FakeGeminiClient:
    def __init__(self, generated_text: str | None = None, available: bool = True):
        self.generated_text = generated_text
        self.available = available

    def is_available(self) -> bool:
        return self.available

    def generate_text(self, prompt: str) -> str | None:
        return self.generated_text


class RaisingGraph:
    def invoke(self, state):
        raise RuntimeError("graph failure")


class FakePagedRealClient:
    def __init__(self):
        self.queries = []
        self.books = {
            "real-hobbit": {
                "id": "real-hobbit",
                "title": "El Hobbit",
                "author": "J. R. R. Tolkien",
                "genre": "Fantasía",
                "price": 50000,
                "available": True,
            },
            "real-harry": {
                "id": "real-harry",
                "title": "Harry Potter y la piedra filosofal",
                "author": "J. K. Rowling",
                "genre": "Fantasía",
                "price": 52000,
                "available": True,
            },
        }
        self.books.update(
            {
                "real-alicia": {
                    "id": "real-alicia",
                    "title": "Alicia en el pa\u00eds de las maravillas",
                    "author": "Lewis Carroll",
                    "genre": "Fantasia",
                    "price": 45000,
                    "available": True,
                },
                "real-lotr": {
                    "id": "real-lotr",
                    "title": "El Se\u00f1or de los Anillos",
                    "author": "J. R. R. Tolkien",
                    "genre": "Fantasia",
                    "price": 78000,
                    "available": True,
                },
            }
        )

    def search_books(self, query: str | None = None):
        self.queries.append(query)

        if query is None:
            return [{"id": "other-book", "title": "Alicia en el país de las maravillas"}]

        normalized_query = query.lower()

        if "hobbit" in normalized_query:
            return [self.books["real-hobbit"]]

        if "harry potter" in normalized_query:
            return [self.books["real-harry"]]

        if "alicia" in normalized_query:
            return [self.books["real-alicia"]]

        if "senor" in normalized_query or "anillos" in normalized_query:
            return [self.books["real-lotr"]]

        return []

    def get_book_detail(self, book_id: str):
        return self.books.get(book_id)


def request(message: str, **overrides) -> ChatProcessRequest:
    payload = {
        "sessionId": "session-graph-123",
        "message": message,
        "userId": "0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        "userEmail": "cliente@example.com",
        "roles": ["CLIENT"],
        "permissions": ["chat.message"],
        "source": "DOTNET_BACKEND",
    }
    payload.update(overrides)
    return ChatProcessRequest(**payload)


def process(message: str, **overrides) -> ChatProcessResponse:
    return ChatGraphService().process(request(message, **overrides))


def test_chat_graph_service_exists_and_returns_response():
    response = process("hola")

    assert isinstance(response, ChatProcessResponse)
    assert response.state == "IDLE"


def test_build_chat_graph_uses_langgraph_and_compiles():
    graph = build_chat_graph(
        PermissionService(),
        ConfirmationService(),
        LlmAssistantService(FakeGeminiClient(available=False)),
        BiblioBotToolService(),
    )

    assert hasattr(graph, "invoke")


def test_chat_graph_process_works_without_gemini_api_key():
    service = ChatGraphService(llm_assistant_service=LlmAssistantService(FakeGeminiClient(available=False)))

    response = service.process(request("hola"))

    assert response.state == "IDLE"
    assert response.context.intent == "general_help"


def test_graph_internal_error_returns_failed_controlled():
    service = ChatGraphService(compiled_graph=RaisingGraph())

    response = service.process(request("hola"))

    assert response.state == "FAILED"
    assert response.uiAction == "NONE"
    assert response.context.intent == "graph_error"


def test_base_validations_remain_controlled():
    assert process("hola", sessionId="").state == "NEEDS_CLARIFICATION"
    assert process("hola", permissions=["books.read"]).state == "FAILED"
    assert process("hola", roles=[]).state == "FAILED"


def test_confirmation_without_pending_action_and_cancel_are_safe():
    confirmation = process("si confirmo")
    cancellation = process("cancelar")

    assert confirmation.state == "NEEDS_CLARIFICATION"
    assert confirmation.context.nextAction == "ASK_ACTION_DETAILS"
    assert cancellation.state == "IDLE"
    assert cancellation.context.nextAction == "WAITING_USER_MESSAGE"


def test_catalog_search_requires_explicit_book_permission():
    denied = process("recomiendame libros de fantasia")
    allowed = process("recomiendame libros de fantasia", permissions=["chat.message", "books.search"])
    admin_without_permission = process("recomiendame libros de fantasia", roles=["ADMIN"])

    assert denied.state == "FAILED"
    assert allowed.state == "INTENT_DETECTED"
    assert admin_without_permission.state == "FAILED"


def test_purchase_intent_requires_purchase_permission():
    response = process("quiero comprar 2 Python Practico")

    assert response.state == "FAILED"
    assert response.context.nextAction == "PERMISSION_DENIED"


def test_catalog_search_uses_mock_and_prepares_visual_navigation():
    response = process("recomiendame libros de fantasia", permissions=["chat.message", "books.search"])

    assert response.state == "INTENT_DETECTED"
    assert response.uiAction == "NAVIGATE_TO_CATALOG"
    assert response.response
    assert response.context.metadata["resultCount"] >= 2
    assert response.context.metadata["filters"]["genre"] == "fantasia"


def test_catalog_result_state_accepts_real_backend_books():
    state = {"metadata": {"detectedIntent": "catalog_search"}}
    result = {
        "status": "REAL_BACKEND",
        "books": [
            {
                "id": "real-book-001",
                "title": "El Hobbit",
                "authors": ["J. R. R. Tolkien"],
                "categories": ["Fantasía"],
                "price": 50000,
                "available": True,
            }
        ],
    }

    response = _catalog_result_state(state, "fantasia", result)

    assert response["state"] == "INTENT_DETECTED"
    assert response["ui_action"] == "NAVIGATE_TO_CATALOG"
    assert response["metadata"]["resultCount"] == 1
    assert response["metadata"]["books"][0]["author"] == "J. R. R. Tolkien"
    assert response["metadata"]["books"][0]["genre"] == "Fantasía"


def test_catalog_search_with_show_me_books_phrase_stays_catalog():
    response = process("muestrame libros de terror", permissions=["chat.message", "books.search"])

    assert response.state == "INTENT_DETECTED"
    assert response.context.intent == "catalog_search"
    assert response.uiAction == "NAVIGATE_TO_CATALOG"
    assert response.context.metadata["filters"]["genre"] == "terror"


def test_book_detail_found_returns_product_navigation_and_link():
    response = process("ver libro Python Practico", permissions=["chat.message", "books.read"])

    assert response.state == "INTENT_DETECTED"
    assert response.uiAction == "NAVIGATE_TO_PRODUCT"
    assert response.context.selectedBookId == "book-003"
    assert response.links[0].url == "/books/python-practico-book-003"
    assert response.links[0].type == "BOOK_DETAIL"


def test_find_book_from_message_uses_specific_query_before_unpaged_fallback():
    client = FakePagedRealClient()
    service = BiblioBotToolService(mock_client=client)

    hobbit = _find_book_from_message("ver libro El Hobbit", service)
    harry = _find_book_from_message("detalle de Harry Potter", service)
    stock = _find_book_from_message("hay stock de El Hobbit", service)

    assert hobbit["title"] == "El Hobbit"
    assert harry["title"] == "Harry Potter y la piedra filosofal"
    assert stock["title"] == "El Hobbit"
    assert client.queries[0] == "el hobbit"
    assert None not in client.queries[:3]


def test_natural_book_detail_phrases_detect_and_extract_clean_title():
    cases = [
        (
            "dime sobre alicia en el pais de las maravillas",
            "alicia en el pais de las maravillas",
            "Alicia en el pa\u00eds de las maravillas",
        ),
        (
            "hablame de El Hobbit",
            "el hobbit",
            "El Hobbit",
        ),
        (
            "que sabes de El Se\u00f1or de los Anillos",
            "el senor de los anillos",
            "El Se\u00f1or de los Anillos",
        ),
    ]

    for message, expected_query, expected_title in cases:
        client = FakePagedRealClient()
        service = BiblioBotToolService(mock_client=client)

        assert _detect_intent(_normalize(message)) == "book_detail"
        assert _extract_book_lookup_queries(message)[0] == expected_query
        assert _find_book_from_message(message, service)["title"] == expected_title
        assert client.queries[0] == expected_query


def test_book_detail_without_identifier_asks_clarification():
    response = process("ver libro", permissions=["chat.message", "books.read"])

    assert response.state == "ASKING_DETAILS"
    assert response.context.nextAction == "ASK_BOOK_IDENTIFIER"


def test_invoice_sales_and_stock_read_tools_use_mock_data():
    invoice = process("muestrame la factura FAC-0001", permissions=["chat.message", "invoices.read_own"])
    sales = process("reporte de ventas", roles=["ADMIN"], permissions=["chat.message", "sales.read_all"])
    stock = process("stock de Python Practico", permissions=["chat.message", "inventory.read"])

    assert invoice.state == "INTENT_DETECTED"
    assert invoice.uiAction == "SHOW_INVOICE"
    assert invoice.context.metadata["invoice"]["id"] == "FAC-0001"
    assert sales.context.metadata["scope"] == "all"
    assert sales.context.metadata["sales"][0]["status"] == "MOCK_ONLY"
    assert stock.context.metadata["stock"]["bookId"] == "book-003"


def test_sensitive_purchase_with_permission_needs_details_or_confirmation():
    missing = process("quiero comprar", permissions=["chat.message", "cart.manage"])
    ready = process("quiero comprar 2 Python Practico", permissions=["chat.message", "cart.manage"])

    assert missing.state == "ASKING_DETAILS"
    assert missing.context.requiresConfirmation is True
    assert ready.state == "WAITING_CONFIRMATION"
    assert ready.context.requiresConfirmation is True
    assert ready.context.metadata["pendingAction"]["status"] == "PENDING_CONFIRMATION"


def test_purchase_intent_extracts_quantity_and_book_for_confirmation():
    response = process("Quiero comprar 2 Python Practico", permissions=["chat.message", "cart.manage"])
    pending_action = response.context.metadata["pendingAction"]

    assert response.state == "WAITING_CONFIRMATION"
    assert response.state not in {"DONE", "EXECUTING_ACTION"}
    assert response.context.intent == "purchase_intent"
    assert response.context.requiresConfirmation is True
    assert response.context.actionRef.startswith("mock-action-")
    assert response.context.selectedBookId == "book-003"
    assert response.context.nextAction == "AWAIT_EXPLICIT_CONFIRMATION"
    assert response.uiAction == "NONE"
    assert pending_action["status"] == "PENDING_CONFIRMATION"
    assert pending_action["quantity"] == 2
    assert pending_action["bookTitle"] == "Python Practico"
    assert pending_action["details"]["bookId"] == "book-003"
    assert pending_action["details"]["quantity"] == 2
    assert pending_action["details"]["bookTitle"] == "Python Practico"
    assert response.context.metadata["bookTitle"] == "Python Practico"
    assert response.context.metadata["quantity"] == 2
    assert "invoice" not in response.context.metadata


def test_purchase_pending_action_is_stored_by_session():
    confirmation_service = ConfirmationService()
    service = ChatGraphService(
        confirmation_service=confirmation_service,
        llm_assistant_service=LlmAssistantService(FakeGeminiClient(available=False)),
    )

    response = service.process(
        request(
            "quiero comprar 2 Python Practico",
            sessionId="purchase-session-store",
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )
    pending_action = confirmation_service.get_pending_action("purchase-session-store")

    assert response.state == "WAITING_CONFIRMATION"
    assert response.context.intent == "purchase_intent"
    assert response.context.requiresConfirmation is True
    assert response.context.actionRef
    assert response.context.metadata["pendingAction"]
    assert pending_action
    assert pending_action["actionRef"] == response.context.actionRef
    assert pending_action["originalIntent"] == "purchase_intent"


def test_explicit_confirmation_consumes_existing_pending_action():
    confirmation_service = ConfirmationService()
    service = ChatGraphService(
        confirmation_service=confirmation_service,
        llm_assistant_service=LlmAssistantService(FakeGeminiClient(available=False)),
    )
    session_id = "purchase-session-confirm"

    purchase = service.process(
        request(
            "quiero comprar 2 Python Practico",
            sessionId=session_id,
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )
    confirmation = service.process(
        request(
            "si confirmo",
            sessionId=session_id,
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )

    assert purchase.state == "WAITING_CONFIRMATION"
    assert confirmation.state == "WAITING_CONFIRMATION"
    assert "Aun no tengo una accion pendiente" not in confirmation.response
    assert confirmation.context.intent == "purchase_intent"
    assert confirmation.context.nextAction == "CONFIRMATION_RECEIVED_MUTATION_BLOCKED"
    assert confirmation.context.metadata["originalIntent"] == "purchase_intent"
    assert confirmation.context.metadata["actionRef"] == purchase.context.actionRef
    assert confirmation.context.metadata["confirmedAction"]["status"] == "CONFIRMED_SAFE_MODE"
    assert confirmation.context.metadata["realBackendMutationBlocked"] is True
    assert confirmation_service.get_pending_action(session_id) is None


def test_confirmation_without_pending_action_still_needs_clarification_for_new_session():
    service = ChatGraphService(llm_assistant_service=LlmAssistantService(FakeGeminiClient(available=False)))

    response = service.process(
        request(
            "si confirmo",
            sessionId="purchase-session-empty",
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )

    assert response.state == "NEEDS_CLARIFICATION"
    assert response.context.intent == "confirmation_without_pending_action"
    assert "Aun no tengo una accion pendiente" in response.response


def test_cancellation_clears_pending_action():
    confirmation_service = ConfirmationService()
    service = ChatGraphService(
        confirmation_service=confirmation_service,
        llm_assistant_service=LlmAssistantService(FakeGeminiClient(available=False)),
    )
    session_id = "purchase-session-cancel"

    purchase = service.process(
        request(
            "quiero comprar 2 Python Practico",
            sessionId=session_id,
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )
    cancellation = service.process(
        request(
            "cancelar",
            sessionId=session_id,
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )
    confirmation = service.process(
        request(
            "si confirmo",
            sessionId=session_id,
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )

    assert purchase.state == "WAITING_CONFIRMATION"
    assert cancellation.state == "IDLE"
    assert cancellation.context.intent == "cancel_confirmation"
    assert confirmation_service.get_pending_action(session_id) is None
    assert confirmation.state == "NEEDS_CLARIFICATION"
    assert confirmation.context.intent == "confirmation_without_pending_action"


def test_pending_actions_are_isolated_by_session():
    confirmation_service = ConfirmationService()
    service = ChatGraphService(
        confirmation_service=confirmation_service,
        llm_assistant_service=LlmAssistantService(FakeGeminiClient(available=False)),
    )

    session_a = "purchase-session-a"
    session_b = "purchase-session-b"
    purchase = service.process(
        request(
            "quiero comprar 2 Python Practico",
            sessionId=session_a,
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )
    wrong_session_confirmation = service.process(
        request(
            "si confirmo",
            sessionId=session_b,
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )
    pending_session_a = confirmation_service.get_pending_action(session_a)
    right_session_confirmation = service.process(
        request(
            "si confirmo",
            sessionId=session_a,
            permissions=["chat.message", "cart.manage", "sales.create"],
        )
    )

    assert purchase.state == "WAITING_CONFIRMATION"
    assert wrong_session_confirmation.state == "NEEDS_CLARIFICATION"
    assert wrong_session_confirmation.context.intent == "confirmation_without_pending_action"
    assert pending_session_a
    assert right_session_confirmation.context.intent == "purchase_intent"
    assert right_session_confirmation.context.metadata["actionRef"] == purchase.context.actionRef
    assert confirmation_service.get_pending_action(session_a) is None


def test_purchase_intent_extracts_supported_buy_phrases():
    messages = [
        "Comprar 1 Python Practico",
        "Agrega 2 Python Practico al carrito",
        "Quiero llevar 3 Python Practico",
        "Añade 1 Python Practico",
        "Añadir 1 Python Practico al carrito",
        "Quiero Python Practico x2",
    ]

    for message in messages:
        response = process(message, permissions=["chat.message", "cart.manage"])

        assert response.state == "WAITING_CONFIRMATION"
        assert response.context.metadata["pendingAction"]["status"] == "PENDING_CONFIRMATION"
        assert response.context.selectedBookId == "book-003"
        assert response.state not in {"DONE", "EXECUTING_ACTION"}


def test_purchase_intent_with_sales_create_permission_prepares_confirmation():
    response = process("Quiero comprar 2 Python Practico", permissions=["chat.message", "sales.create"])

    assert response.state == "WAITING_CONFIRMATION"
    assert response.context.requiresConfirmation is True
    assert response.context.metadata["pendingAction"]["status"] == "PENDING_CONFIRMATION"


def test_purchase_intent_missing_quantity_or_unknown_book_keeps_asking_details():
    messages = [
        "Quiero comprar 2",
        "Quiero comprar Python Practico",
        "Quiero comprar 0 Python Practico",
        "Quiero comprar -1 Python Practico",
        "Quiero Python Practico x0",
        "Quiero Python Practico x-1",
        "Quiero comprar 2 El Hobbit",
    ]

    for message in messages:
        response = process(message, permissions=["chat.message", "cart.manage"])

        assert response.state == "ASKING_DETAILS"
        assert response.context.nextAction == "ASK_BOOK_AND_QUANTITY"
        assert response.context.requiresConfirmation is True
        assert response.context.actionRef is None
        assert response.context.metadata.get("pendingAction") is None


def test_purchase_intent_does_not_confuse_book_id_suffix_with_quantity():
    response = process("Quiero comprar Python Practico book-003", permissions=["chat.message", "cart.manage"])

    assert response.state == "ASKING_DETAILS"
    assert response.context.nextAction == "ASK_BOOK_AND_QUANTITY"
    assert response.context.metadata.get("pendingAction") is None


def test_sensitive_internal_actions_require_confirmation_and_never_done():
    inventory = process(
        "registrar entrada de 3 Python Practico en sede norte",
        roles=["WORKER"],
        permissions=["chat.message", "inventory.entry"],
    )
    transfer = process(
        "crear traslado de 1 Python Practico desde sede norte a sede centro",
        roles=["WORKER"],
        permissions=["chat.message", "requests.transfer.create"],
    )
    purchase = process(
        "solicitud de compra de 2 Python Practico para sede norte",
        roles=["WORKER"],
        permissions=["chat.message", "requests.purchase.create"],
    )

    for response in [inventory, transfer, purchase]:
        assert response.context.requiresConfirmation is True
        assert response.state != "DONE"
        assert response.state != "EXECUTING_ACTION"
        assert response.context.metadata["pendingAction"]["status"] == "PENDING_CONFIRMATION"


def test_sensitive_internal_actions_do_not_invent_missing_branch():
    inventory = process(
        "registrar entrada de 3 Python Practico",
        roles=["WORKER"],
        permissions=["chat.message", "inventory.entry"],
    )
    purchase = process(
        "solicitud de compra de 2 Python Practico",
        roles=["WORKER"],
        permissions=["chat.message", "requests.purchase.create"],
    )

    assert inventory.state == "ASKING_DETAILS"
    assert inventory.context.nextAction == "ASK_INVENTORY_ENTRY_DETAILS"
    assert purchase.state == "ASKING_DETAILS"
    assert purchase.context.nextAction == "ASK_PURCHASE_REQUEST_DETAILS"


def test_unknown_without_gemini_needs_clarification():
    service = ChatGraphService(llm_assistant_service=LlmAssistantService(FakeGeminiClient(available=False)))

    response = service.process(request("me gusta el color azul"))

    assert response.state == "NEEDS_CLARIFICATION"
    assert response.context.intent == "unknown"


def test_gemini_can_suggest_allowed_intent_but_not_skip_permissions_or_confirmations():
    llm = LlmAssistantService(FakeGeminiClient(generated_text="purchase_intent", available=True))
    service = ChatGraphService(llm_assistant_service=llm)

    denied = service.process(request("necesito ese ejemplar"))
    allowed = service.process(request("necesito ese ejemplar", permissions=["chat.message", "cart.manage"]))

    assert denied.state == "FAILED"
    assert denied.context.nextAction == "PERMISSION_DENIED"
    assert allowed.state == "ASKING_DETAILS"
    assert allowed.context.requiresConfirmation is True


def test_gemini_safe_text_does_not_change_critical_context():
    llm = LlmAssistantService(FakeGeminiClient(generated_text="Texto conversacional seguro.", available=True))
    service = ChatGraphService(llm_assistant_service=llm)

    response = service.process(
        request("quiero comprar 2 Python Practico", permissions=["chat.message", "cart.manage"])
    )

    assert response.response == "Texto conversacional seguro."
    assert response.state == "WAITING_CONFIRMATION"
    assert response.uiAction == "NONE"
    assert response.context.requiresConfirmation is True
    assert response.context.actionRef.startswith("mock-action-")


def test_final_safety_blocks_invalid_ui_action_and_dangerous_links():
    state = final_safety_node(
        {
            "intent": "book_detail",
            "state": "INTENT_DETECTED",
            "ui_action": "RUN_SCRIPT",
            "links": [
                ChatLink(label="bad", url="javascript:alert(1)", type="BOOK_DETAIL"),
                ChatLink(label="backend", url="/api/libros/book-001", type="BOOK_DETAIL"),
                ChatLink(label="good", url="/books/python-practico-book-001", type="BOOK_DETAIL"),
            ],
            "context": {},
            "metadata": {"secret": "blocked", "query": "python"},
            "requires_confirmation": False,
            "next_step": "BOOK_DETAIL_READY",
        }
    )

    assert state["ui_action"] == "NONE"
    assert len(state["links"]) == 1
    assert state["links"][0].url == "/books/python-practico-book-001"
    assert "secret" not in state["metadata"]


def test_navigate_to_product_requires_selected_book_or_valid_link():
    state = final_safety_node(
        {
            "intent": "book_detail",
            "state": "INTENT_DETECTED",
            "ui_action": "NAVIGATE_TO_PRODUCT",
            "links": [],
            "context": {},
            "metadata": {},
            "requires_confirmation": False,
            "next_step": "BOOK_DETAIL_READY",
        }
    )

    assert state["ui_action"] == "NONE"


def test_endpoints_and_registry_still_work():
    health = client.get("/health")
    chat = client.post(
        "/chat/process",
        json={
            "sessionId": "session-123",
            "message": "Busco un libro de Python",
            "userId": "0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
            "roles": ["CLIENT"],
            "permissions": ["chat.message", "books.search"],
        },
    )

    assert health.status_code == 200
    assert chat.status_code == 200
    assert chat.json()["context"]["intent"] == "catalog_search"
    assert get_langchain_tools()


def test_graph_source_has_no_openai_db_or_real_http_clients():
    app_dir = Path(__file__).resolve().parents[1] / "app"
    source = "\n".join(path.read_text(encoding="utf-8").lower() for path in app_dir.rglob("*.py"))

    assert "openai" not in source
    assert "psycopg" not in source
    assert "asyncpg" not in source
    assert "sqlalchemy" not in source
    assert "import requests" not in source
    assert "from requests" not in source
