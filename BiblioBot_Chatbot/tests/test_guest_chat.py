from pathlib import Path

import pytest
from fastapi.testclient import TestClient
from pydantic import ValidationError

from app.api.routes import chat_routes
from app.main import app
from app.schemas.chat_contract import ChatProcessRequest
from app.services import LlmAssistantService
from app.tools import BiblioBotToolService, ToolExecutionContext
from app.tools.tool_schemas import SearchBooksInput
from app.tools.tool_schemas import (
    AddOrUpdateCartItemInput,
    ConfirmSaleInput,
    CreatePurchaseRequestInput,
    CreateSaleFromCartInput,
    CreateTransferRequestInput,
    GetInvoiceInput,
    QueryInventoryInput,
    QuerySalesInput,
    RegisterInventoryEntryInput,
)


client = TestClient(app)


class FakeGeminiClient:
    def __init__(self, generated_text: str | None = None, available: bool = True):
        self.generated_text = generated_text
        self.available = available

    def is_available(self) -> bool:
        return self.available

    def generate_text(self, prompt: str) -> str | None:
        return self.generated_text


def payload(message: str, **overrides):
    data = {
        "sessionId": "guest-session-001",
        "message": message,
        "userId": None,
        "userEmail": None,
        "roles": ["GUEST"],
        "permissions": ["chat.message", "books.read", "books.search"],
        "source": "DOTNET_BACKEND",
        "sentAt": "2026-07-07T00:00:00Z",
    }
    data.update(overrides)
    return data


def post_chat(message: str, **overrides):
    response = client.post("/chat/process", json=payload(message, **overrides))
    assert response.status_code == 200
    return response.json()


def assert_auth_required(body, original_intent: str):
    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["context"]["intent"] == "auth_required"
    assert body["context"]["nextAction"] == "AUTH_REQUIRED"
    assert body["uiAction"] == "NONE"
    assert {link["type"] for link in body["links"]} == {"AUTH_LOGIN", "AUTH_REGISTER"}
    assert {link["url"] for link in body["links"]} == {"/auth/login", "/auth/register"}
    assert body["context"]["requiresConfirmation"] is False
    assert body["context"]["metadata"]["originalIntent"] == original_intent
    assert body["context"]["metadata"]["authRequired"] is True
    assert body["context"]["metadata"]["guest"] is True
    assert body["state"] not in {"DONE", "EXECUTING_ACTION"}
    assert body["context"]["metadata"].get("pendingAction") is None


def test_chat_process_request_accepts_user_id_null():
    request = ChatProcessRequest(**payload("recomiendame libros de fantasia"))

    assert request.userId is None
    assert request.userEmail is None


def test_chat_process_request_still_accepts_uuid_user_id():
    request = ChatProcessRequest(
        **payload(
            "hola",
            userId="0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
            userEmail="cliente@example.com",
            roles=["CLIENT"],
        )
    )

    assert str(request.userId) == "0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9"


def test_chat_process_request_still_requires_roles_permissions_and_message():
    with pytest.raises(ValidationError):
        ChatProcessRequest(**{key: value for key, value in payload("hola").items() if key != "roles"})
    with pytest.raises(ValidationError):
        ChatProcessRequest(**{key: value for key, value in payload("hola").items() if key != "permissions"})
    with pytest.raises(ValidationError):
        ChatProcessRequest(**payload(""))


def test_guest_can_search_catalog_and_get_navigation_metadata():
    body = post_chat("recomiendame libros de fantasia")

    assert body["state"] == "INTENT_DETECTED"
    assert body["context"]["intent"] == "catalog_search"
    assert body["uiAction"] == "NAVIGATE_TO_CATALOG"
    assert body["context"]["metadata"]["guest"] is True
    assert body["context"]["metadata"]["filters"]["genre"] == "fantasia"


def test_guest_can_view_book_detail_and_product_link():
    body = post_chat("ver libro Python Practico")

    assert body["state"] == "INTENT_DETECTED"
    assert body["context"]["intent"] == "book_detail"
    assert body["uiAction"] == "NAVIGATE_TO_PRODUCT"
    assert body["context"]["selectedBookId"] == "book-003"
    assert body["links"][0]["url"] == "/books/python-practico-book-003"


def test_guest_purchase_intent_returns_auth_required():
    body = post_chat("quiero comprar 2 Python Practico")

    assert_auth_required(body, "purchase_intent")


def test_phase11_guest_purchase_matilda_returns_auth_required():
    body = post_chat("quiero comprar matilda")

    assert_auth_required(body, "purchase_intent")


def test_phase11_guest_stock_question_is_allowed():
    body = post_chat("tienes matilda?")

    assert body["state"] == "INTENT_DETECTED"
    assert body["context"]["intent"] == "stock_check"
    assert body["context"]["metadata"]["stock"]["title"] == "Matilda"


def test_guest_checkout_cart_returns_auth_required():
    body = post_chat("finalizar compra")

    assert_auth_required(body, "checkout_cart")
    assert body["context"]["metadata"].get("pendingAction") is None


def test_guest_confirm_sale_returns_auth_required():
    body = post_chat("confirmar venta")

    assert_auth_required(body, "confirm_sale")
    assert body["context"]["metadata"].get("pendingAction") is None


def test_guest_invoice_sales_inventory_transfer_and_purchase_request_return_auth_required():
    cases = [
        ("muestrame la factura FAC-0001", "invoice_query"),
        ("reporte de ventas", "sales_query"),
        ("registrar entrada de 3 Python Practico en sede norte", "inventory_entry"),
        ("crear traslado de 1 Python Practico desde sede norte a sede centro", "transfer_request"),
        ("solicitud de compra de 2 Python Practico para sede norte", "purchase_request"),
    ]

    for message, intent in cases:
        assert_auth_required(post_chat(message), intent)


def test_authenticated_client_without_purchase_permission_gets_permission_denied_not_auth_required():
    body = post_chat(
        "quiero comprar 2 Python Practico",
        userId="0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        userEmail="cliente@example.com",
        roles=["CLIENT"],
        permissions=["chat.message", "books.read"],
    )

    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "purchase_intent"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"
    assert body["links"] == []


def test_authenticated_client_with_purchase_permission_keeps_existing_confirmation_flow():
    body = post_chat(
        "quiero comprar 2 Python Practico",
        userId="0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        userEmail="cliente@example.com",
        roles=["CLIENT"],
        permissions=["chat.message", "books.read", "cart.manage"],
    )

    assert body["state"] == "WAITING_CONFIRMATION"
    assert body["context"]["intent"] == "purchase_intent"
    assert body["context"]["requiresConfirmation"] is True
    assert body["context"]["metadata"]["pendingAction"]["status"] == "PENDING_CONFIRMATION"


def test_admin_without_explicit_permissions_still_blocked_by_permissions():
    body = post_chat(
        "reporte de ventas",
        userId="0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        roles=["ADMIN"],
        permissions=["chat.message"],
    )

    assert body["state"] == "FAILED"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_authenticated_user_with_invoice_permission_can_query_mock_invoice():
    body = post_chat(
        "muestrame la factura FAC-0001",
        userId="0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        roles=["CLIENT"],
        permissions=["chat.message", "invoices.read_own"],
    )

    assert body["state"] == "INTENT_DETECTED"
    assert body["context"]["intent"] == "invoice_query"
    assert body["context"]["metadata"]["invoice"]["id"] == "FAC-0001"


def test_confirm_and_cancel_regressions_stay_safe_with_guest_contract():
    confirmation = post_chat("si confirmo", sessionId="guest-confirmation-empty-session")
    cancellation = post_chat("cancelar", sessionId="guest-cancellation-empty-session")

    assert confirmation["state"] == "NEEDS_CLARIFICATION"
    assert confirmation["context"]["nextAction"] == "ASK_ACTION_DETAILS"
    assert cancellation["state"] == "IDLE"
    assert cancellation["context"]["nextAction"] == "WAITING_USER_MESSAGE"


def test_gemini_suggested_protected_intent_still_returns_auth_required_for_guest():
    original_llm_service = chat_routes.chat_orchestrator.llm_assistant_service
    chat_routes.chat_orchestrator.llm_assistant_service = LlmAssistantService(
        FakeGeminiClient(generated_text="purchase_intent", available=True)
    )
    try:
        body = post_chat("necesito ese ejemplar para mi casa")
        assert_auth_required(body, "purchase_intent")
    finally:
        chat_routes.chat_orchestrator.llm_assistant_service = original_llm_service


def test_read_tools_do_not_break_with_none_user_id():
    service = BiblioBotToolService()
    context = ToolExecutionContext(
        session_id="guest-session-001",
        user_id=None,
        roles=["GUEST"],
        permissions=["books.search"],
    )

    result = service.search_books(SearchBooksInput(query="fantasia"), context)

    assert result["status"] == "MOCK_ONLY"
    assert result["resultCount"] >= 1


def test_protected_tools_return_auth_required_for_guest_even_with_accidental_permissions():
    service = BiblioBotToolService()
    context = ToolExecutionContext(
        session_id="guest-session-001",
        user_id=None,
        roles=["GUEST"],
        permissions=[
            "cart.read",
            "cart.manage",
            "sales.create",
            "sales.confirm",
            "sales.read_all",
            "invoices.read_all",
            "inventory.read",
            "inventory.entry",
            "requests.purchase.create",
            "requests.transfer.create",
        ],
    )
    results = [
        service.add_or_update_cart_item(
            AddOrUpdateCartItemInput(session_id="guest-session-001", book_id="book-001", quantity=1),
            context,
        ),
        service.create_sale_from_cart(CreateSaleFromCartInput(session_id="guest-session-001"), context),
        service.confirm_sale(ConfirmSaleInput(sale_id="sale-001"), context),
        service.get_invoice(GetInvoiceInput(invoice_id="FAC-0001"), context),
        service.query_sales(QuerySalesInput(scope="all"), context),
        service.query_inventory(QueryInventoryInput(), context),
        service.register_inventory_entry(
            RegisterInventoryEntryInput(book_id="book-001", branch_id="branch-north", quantity=1),
            context,
        ),
        service.create_purchase_request(
            CreatePurchaseRequestInput(branch_id="branch-north", book_id="book-001", quantity=1),
            context,
        ),
        service.create_transfer_request(
            CreateTransferRequestInput(
                source_branch_id="branch-north",
                destination_branch_id="branch-center",
                book_id="book-001",
                quantity=1,
            ),
            context,
        ),
    ]

    for result in results:
        assert result["status"] == "AUTH_REQUIRED"
        assert result["mode"] == "BLOCKED"
        assert result["requiresAuthentication"] is True
        assert {link["type"] for link in result["links"]} == {"AUTH_LOGIN", "AUTH_REGISTER"}
        assert "pendingAction" not in result


def test_health_and_chat_process_still_work():
    health = client.get("/health")
    chat = client.post("/chat/process", json=payload("hola"))

    assert health.status_code == 200
    assert health.json()["status"] == "ok"
    assert chat.status_code == 200


def test_no_openai_db_http_or_real_mutation_code_added_for_guest_phase():
    app_dir = Path(__file__).resolve().parents[1] / "app"
    source = "\n".join(path.read_text(encoding="utf-8").lower() for path in app_dir.rglob("*.py"))

    assert "openai" not in source
    assert "psycopg" not in source
    assert "asyncpg" not in source
    assert "sqlalchemy" not in source
    assert "import requests" not in source
    assert "from requests" not in source
