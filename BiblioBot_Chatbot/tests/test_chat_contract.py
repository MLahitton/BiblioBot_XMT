from fastapi.testclient import TestClient

from app.clients import MockDotNetClient
from app.main import app
from app.services import ConfirmationService, PermissionService


client = TestClient(app)


def build_payload(message: str, **overrides):
    payload = {
        "sessionId": "session-123",
        "message": message,
        "userId": "0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        "userEmail": "cliente@example.com",
        "roles": ["CLIENT"],
        "permissions": ["chat.message"],
        "source": "DOTNET_BACKEND",
        "sentAt": "2026-07-06T12:00:00Z",
    }
    payload.update(overrides)
    return payload


def test_permission_service_has_permission_true():
    service = PermissionService()

    assert service.has_permission(["books.read"], "books.read") is True


def test_permission_service_has_permission_false():
    service = PermissionService()

    assert service.has_permission(["books.read"], "sales.create") is False


def test_permission_service_has_any_permission():
    service = PermissionService()

    assert service.has_any_permission(["sales.read_own"], ["sales.read_all", "sales.read_own"]) is True


def test_permission_service_required_permissions_for_intents():
    service = PermissionService()

    assert service.required_permissions_for_intent("purchase_intent") == ["cart.manage", "sales.create"]
    assert service.required_permissions_for_intent("inventory_entry") == ["inventory.entry"]
    assert service.required_permissions_for_intent("sales_query") == ["sales.read_own", "sales.read_all"]
    assert service.required_permissions_for_intent("catalog_search") == ["books.read", "books.search"]


def test_permission_service_does_not_authorize_admin_without_permissions():
    service = PermissionService()

    assert service.can_access_intent("sales_query", []) is False


def test_confirmation_service_purchase_requires_confirmation():
    service = ConfirmationService()

    assert service.requires_confirmation("purchase_intent") is True


def test_confirmation_service_catalog_does_not_require_confirmation():
    service = ConfirmationService()

    assert service.requires_confirmation("catalog_search") is False


def test_confirmation_service_detects_confirmation():
    service = ConfirmationService()

    assert service.is_explicit_confirmation("si confirmo") is True


def test_confirmation_service_detects_cancellation():
    service = ConfirmationService()

    assert service.is_explicit_cancellation("cancelar") is True


def test_confirmation_service_builds_action_ref():
    service = ConfirmationService()

    action_ref = service.build_action_ref("session-123", "purchase_intent", "comprar libro")

    assert action_ref.startswith("mock-action-")
    assert len(action_ref) > len("mock-action-")


def test_chat_process_accepts_dotnet_contract():
    payload = build_payload(
        "Quiero buscar libros de arquitectura",
        permissions=["books.read", "books.search", "chat.message"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert set(body.keys()) == {"response", "state", "links", "uiAction", "context"}
    assert body["state"] == "INTENT_DETECTED"
    assert body["links"] == []
    assert body["uiAction"] == "NAVIGATE_TO_CATALOG"
    assert body["context"]["intent"] == "catalog_search"
    assert body["context"]["requiresConfirmation"] is False
    assert body["context"]["saleOrigin"] == "CHATBOT"
    assert body["context"]["metadata"]["sessionId"] == payload["sessionId"]
    assert body["context"]["metadata"]["detectedIntent"] == "catalog_search"


def test_chat_process_rejects_invalid_dotnet_contract():
    payload = {
        "message": "",
        "userId": "not-a-uuid",
        "roles": ["CLIENT"],
        "permissions": ["chat.message"],
    }

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 422


def test_chat_process_response_keeps_future_fields_inside_context():
    payload = build_payload(
        "Necesito confirmar una compra",
        sessionId="session-456",
        userId="26f79d05-a18a-4a3a-94c0-e581e9ba1d3b",
        permissions=["cart.manage", "sales.create", "chat.message"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert "requiresConfirmation" not in body
    assert "actionRef" not in body
    assert "invoiceNumber" not in body
    assert "nextAction" not in body
    assert "requiresConfirmation" in body["context"]
    assert "nextAction" in body["context"]


def test_chat_process_without_chat_permission_fails():
    payload = build_payload("Hola", permissions=["books.read"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "permission_denied"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_chat_process_empty_session_needs_clarification():
    payload = build_payload("Hola", sessionId="")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["context"]["intent"] == "missing_session"
    assert body["context"]["nextAction"] == "REQUEST_VALID_SESSION"


def test_chat_process_missing_session_needs_clarification():
    payload = build_payload("Hola")
    payload.pop("sessionId")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["context"]["intent"] == "missing_session"


def test_chat_process_empty_roles_fails():
    payload = build_payload("Hola", roles=[])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "missing_roles"
    assert body["context"]["nextAction"] == "REQUEST_VALID_ROLE"


def test_catalog_search_intent():
    payload = build_payload("Busco un libro de Python", permissions=["chat.message", "books.search"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["context"]["intent"] == "catalog_search"
    assert body["state"] == "INTENT_DETECTED"
    assert body["uiAction"] == "NAVIGATE_TO_CATALOG"


def test_catalog_search_without_book_permissions_fails():
    payload = build_payload("Busco un libro de Python", permissions=["chat.message"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "catalog_search"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_catalog_search_handles_accented_recommendation():
    payload = build_payload(
        "recomiendame libros de fantasia",
        permissions=["chat.message", "books.search"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "INTENT_DETECTED"
    assert body["context"]["intent"] == "catalog_search"
    assert body["uiAction"] == "NAVIGATE_TO_CATALOG"
    assert body["context"]["metadata"]["resultCount"] >= 2
    assert body["context"]["metadata"]["books"]


def test_mock_client_search_books_returns_fantasy_books():
    mock_client = MockDotNetClient()

    books = mock_client.search_books("fantasia")

    assert len(books) >= 2
    assert all(book["genre"] == "fantasia" for book in books)


def test_mock_client_get_book_detail_returns_existing_book():
    mock_client = MockDotNetClient()

    book = mock_client.get_book_detail("book-001")

    assert book is not None
    assert book["id"] == "book-001"
    assert "stockByBranch" in book


def test_mock_client_get_book_detail_returns_none_for_missing_book():
    mock_client = MockDotNetClient()

    book = mock_client.get_book_detail("book-missing")

    assert book is None


def test_mock_client_check_stock_returns_branch_stock():
    mock_client = MockDotNetClient()

    stock = mock_client.check_stock("book-001", "branch-north")

    assert stock is not None
    assert stock["bookId"] == "book-001"
    assert stock["branchId"] == "branch-north"
    assert stock["stock"] == 4
    assert stock["status"] == "MOCK_ONLY"


def test_mock_client_check_stock_handles_missing_branch():
    mock_client = MockDotNetClient()

    stock = mock_client.check_stock("book-001", "branch-missing")

    assert stock is not None
    assert stock["branchId"] == "branch-missing"
    assert stock["stock"] == 0
    assert stock["available"] is False
    assert stock["status"] == "MOCK_ONLY"


def test_book_detail_detects_existing_book_from_message():
    payload = build_payload("ver libro Python Practico", permissions=["chat.message", "books.read"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "INTENT_DETECTED"
    assert body["uiAction"] == "NAVIGATE_TO_PRODUCT"
    assert body["context"]["intent"] == "book_detail"
    assert body["context"]["metadata"]["book"]["id"] == "book-003"


def test_book_detail_without_book_identifier_asks_for_details():
    payload = build_payload("ver libro", permissions=["chat.message", "books.read"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "ASKING_DETAILS"
    assert body["context"]["intent"] == "book_detail"
    assert body["context"]["nextAction"] == "ASK_BOOK_IDENTIFIER"


def test_stock_check_detects_existing_book_from_message():
    payload = build_payload("stock de Python Practico", permissions=["chat.message", "books.read"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "INTENT_DETECTED"
    assert body["context"]["intent"] == "stock_check"
    assert body["context"]["metadata"]["stock"]["bookId"] == "book-003"


def test_stock_check_without_book_asks_book_and_branch():
    payload = build_payload("hay disponibilidad", permissions=["chat.message", "inventory.read"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "ASKING_DETAILS"
    assert body["context"]["intent"] == "stock_check"
    assert body["context"]["nextAction"] == "ASK_BOOK_AND_BRANCH"


def test_purchase_intent_asks_for_book_and_quantity():
    payload = build_payload("Quiero comprar", permissions=["chat.message", "cart.manage"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["context"]["intent"] == "purchase_intent"
    assert body["state"] == "ASKING_DETAILS"
    assert body["state"] != "DONE"
    assert body["context"]["nextAction"] == "ASK_BOOK_AND_QUANTITY"
    assert body["context"]["requiresConfirmation"] is True


def test_purchase_intent_without_purchase_permissions_fails():
    payload = build_payload("Quiero comprar", permissions=["chat.message"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "purchase_intent"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_purchase_intent_with_details_waits_for_confirmation():
    payload = build_payload(
        "quiero comprar 2 Python Practico",
        permissions=["chat.message", "cart.manage"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "WAITING_CONFIRMATION"
    assert body["state"] != "DONE"
    assert body["context"]["intent"] == "purchase_intent"
    assert body["context"]["requiresConfirmation"] is True
    assert body["context"]["actionRef"].startswith("mock-action-")
    assert body["context"]["nextAction"] == "AWAIT_EXPLICIT_CONFIRMATION"
    assert body["context"]["metadata"]["pendingAction"]["status"] == "PENDING_CONFIRMATION"
    assert "invoice" not in body["context"]["metadata"]


def test_inventory_entry_without_permission_fails():
    payload = build_payload(
        "Registrar entrada de inventario",
        roles=["WORKER"],
        permissions=["chat.message"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "inventory_entry"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_inventory_entry_with_permission_asks_details():
    payload = build_payload(
        "Registrar entrada de inventario",
        roles=["WORKER"],
        permissions=["chat.message", "inventory.entry"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "ASKING_DETAILS"
    assert body["context"]["intent"] == "inventory_entry"
    assert body["context"]["nextAction"] == "ASK_INVENTORY_ENTRY_DETAILS"
    assert body["context"]["requiresConfirmation"] is True


def test_transfer_request_with_permission_asks_details_without_mutation():
    payload = build_payload(
        "crear traslado",
        roles=["WORKER"],
        permissions=["chat.message", "requests.transfer.create"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "ASKING_DETAILS"
    assert body["context"]["intent"] == "transfer_request"
    assert body["context"]["nextAction"] == "ASK_TRANSFER_DETAILS"
    assert body["context"]["requiresConfirmation"] is True


def test_purchase_request_with_permission_asks_details_without_mutation():
    payload = build_payload(
        "solicitud de compra",
        roles=["WORKER"],
        permissions=["chat.message", "requests.purchase.create"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "ASKING_DETAILS"
    assert body["context"]["intent"] == "purchase_request"
    assert body["context"]["nextAction"] == "ASK_PURCHASE_REQUEST_DETAILS"
    assert body["context"]["requiresConfirmation"] is True


def test_purchase_request_wins_over_purchase_intent_without_permission():
    payload = build_payload(
        "quiero comprar inventario para la sede",
        roles=["WORKER"],
        permissions=["chat.message"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "purchase_request"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_invoice_query_wins_over_catalog_terms():
    payload = build_payload(
        "muestrame la factura FAC-0001 del libro",
        permissions=["chat.message", "invoices.read_own"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "INTENT_DETECTED"
    assert body["context"]["intent"] == "invoice_query"
    assert body["context"]["nextAction"] == "INVOICE_READY"
    assert body["uiAction"] == "SHOW_INVOICE"
    assert body["links"][0]["type"] == "invoice"
    assert body["context"]["metadata"]["invoice"]["id"] == "FAC-0001"


def test_invoice_query_without_invoice_permissions_fails():
    payload = build_payload("muestrame la factura FAC-0001", permissions=["chat.message"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "invoice_query"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_invoice_query_with_permission_without_id_asks_details():
    payload = build_payload("quiero ver una factura", permissions=["chat.message", "invoices.read_own"])

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "ASKING_DETAILS"
    assert body["context"]["intent"] == "invoice_query"
    assert body["context"]["requiresConfirmation"] is False


def test_sales_query_with_read_all_returns_mock_sales():
    payload = build_payload(
        "reporte de ventas",
        roles=["ADMIN"],
        permissions=["chat.message", "sales.read_all"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "INTENT_DETECTED"
    assert body["context"]["intent"] == "sales_query"
    assert body["context"]["metadata"]["scope"] == "all"
    assert body["context"]["metadata"]["sales"]
    assert body["context"]["metadata"]["sales"][0]["status"] == "MOCK_ONLY"


def test_sales_query_with_read_own_returns_mock_sales():
    payload = build_payload(
        "mis ventas",
        roles=["CLIENT"],
        permissions=["chat.message", "sales.read_own"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "INTENT_DETECTED"
    assert body["context"]["intent"] == "sales_query"
    assert body["context"]["metadata"]["scope"] == "own"
    assert body["context"]["metadata"]["sales"]


def test_sales_query_without_read_permissions_fails():
    payload = build_payload(
        "reporte de ventas",
        roles=["WORKER"],
        permissions=["chat.message"],
    )

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "sales_query"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_unknown_intent_needs_clarification():
    payload = build_payload("Me gusta el color azul")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["context"]["intent"] == "unknown"
    assert body["context"]["nextAction"] == "ASK_CLARIFICATION"


def test_out_of_domain_question_needs_clarification():
    payload = build_payload("quien gano el mundial 2014")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["context"]["intent"] == "unknown"
    assert body["context"]["nextAction"] == "ASK_CLARIFICATION"


def test_confirmation_without_pending_action_needs_clarification():
    payload = build_payload("si confirmo")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["context"]["intent"] == "confirmation_without_pending_action"
    assert body["context"]["nextAction"] == "ASK_ACTION_DETAILS"


def test_cancel_confirmation_returns_idle():
    payload = build_payload("cancelar")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "IDLE"
    assert body["context"]["intent"] == "cancel_confirmation"
    assert body["context"]["nextAction"] == "WAITING_USER_MESSAGE"


def test_chat_process_response_shape_remains_compatible():
    payload = build_payload("Hola")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert isinstance(body["response"], str)
    assert "state" in body
    assert isinstance(body["links"], list)
    assert "uiAction" in body
    assert isinstance(body["context"], dict)


def test_health_still_works():
    response = client.get("/health")

    assert response.status_code == 200
    assert response.json()["status"] == "ok"
