from pathlib import Path

from fastapi.testclient import TestClient

from app.main import app
from app.services import FrontendActionService


client = TestClient(app)


def payload(message: str, **overrides):
    data = {
        "sessionId": "visual-session-001",
        "message": message,
        "userId": "0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        "userEmail": "cliente@example.com",
        "roles": ["CLIENT"],
        "permissions": ["chat.message"],
        "source": "DOTNET_BACKEND",
        "sentAt": "2026-07-07T00:00:00Z",
    }
    data.update(overrides)
    return data


def post_chat(message: str, **overrides):
    response = client.post("/chat/process", json=payload(message, **overrides))
    assert response.status_code == 200
    return response.json()


def test_auth_required_uses_frontend_auth_routes_and_no_new_ui_action():
    body = post_chat(
        "Quiero comprar 2 Python Practico",
        userId=None,
        userEmail=None,
        roles=["GUEST"],
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["context"]["intent"] == "auth_required"
    assert body["context"]["nextAction"] == "AUTH_REQUIRED"
    assert body["uiAction"] == "NONE"
    assert {link["url"] for link in body["links"]} == {"/auth/login", "/auth/register"}
    assert "/login" not in {link["url"] for link in body["links"]}
    assert "/register" not in {link["url"] for link in body["links"]}


def test_catalog_search_returns_search_route_metadata_and_safe_link():
    body = post_chat("Recomiendame libros de fantasia", permissions=["chat.message", "books.search"])
    metadata = body["context"]["metadata"]

    assert body["state"] == "INTENT_DETECTED"
    assert body["uiAction"] == "NAVIGATE_TO_CATALOG"
    assert metadata["frontendRoute"] == "/search"
    assert metadata["filters"]["genre"] == "fantasia"
    assert metadata["genre"] == "fantasia"
    assert body["links"][0]["type"] == "CATALOG_SEARCH"
    assert body["links"][0]["url"].startswith("/search")
    assert not body["links"][0]["url"].startswith("/api/")


def test_book_detail_returns_books_slug_metadata_and_link():
    body = post_chat("ver libro Python Practico", permissions=["chat.message", "books.read"])
    metadata = body["context"]["metadata"]

    assert body["state"] == "INTENT_DETECTED"
    assert body["uiAction"] == "NAVIGATE_TO_PRODUCT"
    assert body["context"]["selectedBookId"] == "book-003"
    assert metadata["selectedBookId"] == "book-003"
    assert metadata["bookTitle"] == "Python Practico"
    assert metadata["slug"] == "python-practico-book-003"
    assert metadata["frontendRoute"] == "/books/python-practico-book-003"
    assert body["links"][0]["type"] == "BOOK_DETAIL"
    assert body["links"][0]["url"] == "/books/python-practico-book-003"
    assert "/libros/" not in body["links"][0]["url"]
    assert "/api/libros" not in body["links"][0]["url"]


def test_book_detail_without_valid_book_does_not_navigate_to_product():
    body = post_chat("ver libro El Hobbit", permissions=["chat.message", "books.read"])

    assert body["state"] == "ASKING_DETAILS"
    assert body["uiAction"] == "NONE"
    assert body["links"] == []


def test_show_invoice_keeps_visual_action_without_invented_frontend_route():
    body = post_chat("muestrame la factura FAC-0001", permissions=["chat.message", "invoices.read_own"])
    metadata = body["context"]["metadata"]

    assert body["state"] == "INTENT_DETECTED"
    assert body["uiAction"] == "SHOW_INVOICE"
    assert body["links"] == []
    assert body["context"]["invoiceNumber"] == "FAC-0001"
    assert metadata["invoiceNumber"] == "FAC-0001"
    assert metadata["saleId"] == "sale-001"


def test_general_help_returns_initial_suggestions_inside_metadata_only():
    body = post_chat("hola")
    suggestions = body["context"]["metadata"]["suggestions"]

    assert body["state"] == "IDLE"
    assert "suggestions" not in body
    assert isinstance(suggestions, list)
    assert suggestions
    assert "Comprar ahora" not in suggestions
    assert all(isinstance(suggestion, str) and suggestion.strip() for suggestion in suggestions)
    assert all("comprar" not in suggestion.lower() for suggestion in suggestions)


def test_frontend_action_service_blocks_unsafe_paths_and_backend_routes():
    service = FrontendActionService()

    assert service.sanitize_internal_path("javascript:alert(1)") is None
    assert service.sanitize_internal_path("http://example.com") is None
    assert service.sanitize_internal_path("https://example.com") is None
    assert service.sanitize_internal_path("data:text/html,<script>alert(1)</script>") is None
    assert service.sanitize_internal_path("//evil.example/path") is None
    assert service.sanitize_internal_path("/books\\python-practico-book-003") is None
    assert service.sanitize_internal_path("/api/libros") is None
    assert service.sanitize_internal_path("/C:/Users/USUARIO/secrets.txt") is None
    assert service.sanitize_internal_path("/file:/tmp/secrets.txt") is None
    assert service.sanitize_internal_path("/search?q=fantasia") == "/search?q=fantasia"
    assert service.sanitize_internal_path("/books/python-practico-book-001") == "/books/python-practico-book-001"
    assert service.sanitize_internal_path("/auth/login") == "/auth/login"
    assert service.sanitize_internal_path("/auth/register") == "/auth/register"
    assert service.build_cart_link().url == "/cart"


def test_regressions_health_permissions_guest_and_pending_purchase_stay_safe():
    health = client.get("/health")
    guest = post_chat(
        "Quiero comprar 2 Python Practico",
        userId=None,
        userEmail=None,
        roles=["GUEST"],
        permissions=["chat.message", "books.read", "books.search"],
    )
    denied = post_chat("Quiero comprar 2 Python Practico", permissions=["chat.message", "books.read"])
    purchase = post_chat("Quiero comprar 2 Python Practico", permissions=["chat.message", "cart.manage"])

    assert health.status_code == 200
    assert guest["context"]["nextAction"] == "AUTH_REQUIRED"
    assert denied["context"]["nextAction"] == "PERMISSION_DENIED"
    assert purchase["state"] == "WAITING_CONFIRMATION"
    assert purchase["context"]["metadata"]["pendingAction"]["status"] == "PENDING_CONFIRMATION"
    assert purchase["state"] not in {"DONE", "EXECUTING_ACTION"}


def test_no_openai_db_http_or_real_mutation_code_added_for_visual_phase():
    app_dir = Path(__file__).resolve().parents[1] / "app"
    source = "\n".join(path.read_text(encoding="utf-8").lower() for path in app_dir.rglob("*.py"))

    assert "openai" not in source
    assert "psycopg" not in source
    assert "asyncpg" not in source
    assert "sqlalchemy" not in source
    assert "import requests" not in source
    assert "from requests" not in source
    assert "import httpx" not in source
    assert "from httpx" not in source
