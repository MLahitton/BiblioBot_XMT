import json
from pathlib import Path

import pytest
from fastapi.testclient import TestClient
from pydantic import ValidationError

from app.main import app
from app.services import PermissionService
from app.tools import BiblioBotToolService, ToolExecutionContext, get_langchain_tools
from app.tools.tool_schemas import (
    AddOrUpdateCartItemInput,
    CheckStockInput,
    ConfirmSaleInput,
    CreatePurchaseRequestInput,
    CreateSaleFromCartInput,
    CreateTransferRequestInput,
    GetBookDetailInput,
    GetInvoiceInput,
    QueryInventoryInput,
    QuerySalesInput,
    RegisterInventoryEntryInput,
    SearchBooksInput,
)


client = TestClient(app)


def context_with(*permissions: str) -> ToolExecutionContext:
    return ToolExecutionContext(
        session_id="session-tools-123",
        user_id="0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        roles=["ADMIN"],
        permissions=list(permissions),
    )


def assert_json_serializable(value):
    json.dumps(value)


def test_add_or_update_cart_item_rejects_non_positive_quantity():
    with pytest.raises(ValidationError):
        AddOrUpdateCartItemInput(session_id="session-1", book_id="book-001", quantity=0)


def test_register_inventory_entry_rejects_non_positive_quantity():
    with pytest.raises(ValidationError):
        RegisterInventoryEntryInput(book_id="book-001", branch_id="branch-north", quantity=0)


def test_query_sales_rejects_invalid_scope():
    with pytest.raises(ValidationError):
        QuerySalesInput(scope="global")


def test_create_transfer_request_requires_source_and_destination_branch():
    with pytest.raises(ValidationError):
        CreateTransferRequestInput(
            source_branch_id="",
            destination_branch_id="branch-south",
            book_id="book-001",
            quantity=1,
        )

    with pytest.raises(ValidationError):
        CreateTransferRequestInput(
            source_branch_id="branch-north",
            destination_branch_id="",
            book_id="book-001",
            quantity=1,
        )


def test_empty_session_id_fails_for_required_session_tools():
    with pytest.raises(ValidationError):
        AddOrUpdateCartItemInput(session_id="", book_id="book-001", quantity=1)


def test_empty_book_id_fails_for_required_book_tools():
    with pytest.raises(ValidationError):
        GetBookDetailInput(book_id="")

    with pytest.raises(ValidationError):
        AddOrUpdateCartItemInput(session_id="session-1", book_id="", quantity=1)


def test_empty_branch_id_fails_when_required():
    with pytest.raises(ValidationError):
        RegisterInventoryEntryInput(book_id="book-001", branch_id="", quantity=1)

    with pytest.raises(ValidationError):
        CreatePurchaseRequestInput(branch_id="", book_id="book-001", quantity=1)


def test_search_books_with_permission_returns_mock_results():
    service = BiblioBotToolService()

    result = service.search_books(SearchBooksInput(query="fantasia"), context_with("books.search"))

    assert result["status"] == "MOCK_ONLY"
    assert result["mode"] == "READ_ONLY"
    assert result["resultCount"] >= 1
    assert_json_serializable(result)


def test_search_books_without_permission_is_denied():
    service = BiblioBotToolService()

    result = service.search_books(SearchBooksInput(query="python"), context_with("chat.message"))

    assert result["status"] == "PERMISSION_DENIED"
    assert result["requiredPermissions"] == ["books.read", "books.search"]


def test_get_book_detail_with_permission_returns_mock_book():
    service = BiblioBotToolService()

    result = service.get_book_detail(GetBookDetailInput(book_id="book-001"), context_with("books.read"))

    assert result["status"] == "MOCK_ONLY"
    assert result["book"]["id"] == "book-001"


def test_get_book_detail_without_books_read_is_denied():
    service = BiblioBotToolService()

    result = service.get_book_detail(GetBookDetailInput(book_id="book-001"), context_with("books.search"))

    assert result["status"] == "PERMISSION_DENIED"
    assert result["requiredPermissions"] == ["books.read"]


def test_check_stock_with_inventory_permission_returns_mock_stock():
    service = BiblioBotToolService()

    result = service.check_stock(
        CheckStockInput(book_id="book-001", branch_id="branch-north"),
        context_with("inventory.read"),
    )

    assert result["status"] == "MOCK_ONLY"
    assert result["stock"]["bookId"] == "book-001"
    assert result["stock"]["branchId"] == "branch-north"


def test_get_invoice_with_permission_returns_mock_invoice():
    service = BiblioBotToolService()

    result = service.get_invoice(GetInvoiceInput(invoice_id="FAC-0001"), context_with("invoices.read_own"))

    assert result["status"] == "MOCK_ONLY"
    assert result["invoice"]["id"] == "FAC-0001"


def test_query_sales_with_read_all_uses_all_scope():
    service = BiblioBotToolService()

    result = service.query_sales(QuerySalesInput(scope="all"), context_with("sales.read_all"))

    assert result["status"] == "MOCK_ONLY"
    assert result["scope"] == "all"
    assert all(sale["scope"] == "all" for sale in result["sales"])


def test_query_sales_all_without_read_all_is_denied():
    service = BiblioBotToolService()

    result = service.query_sales(QuerySalesInput(scope="all"), context_with("sales.read_own"))

    assert result["status"] == "PERMISSION_DENIED"
    assert result["requiredPermissions"] == ["sales.read_all"]


def test_query_sales_with_read_own_uses_own_scope():
    service = BiblioBotToolService()

    result = service.query_sales(QuerySalesInput(scope="own"), context_with("sales.read_own"))

    assert result["status"] == "MOCK_ONLY"
    assert result["scope"] == "own"
    assert all(sale["scope"] == "own" for sale in result["sales"])


def test_query_inventory_with_inventory_read_returns_low_stock_mock():
    service = BiblioBotToolService()

    result = service.query_inventory(QueryInventoryInput(only_low_stock=True), context_with("inventory.read"))

    assert result["status"] == "MOCK_ONLY"
    assert result["mode"] == "READ_ONLY"
    assert result["onlyLowStock"] is True


def test_add_or_update_cart_item_without_permission_is_denied():
    service = BiblioBotToolService()

    result = service.add_or_update_cart_item(
        AddOrUpdateCartItemInput(session_id="session-1", book_id="book-001", quantity=1),
        context_with("chat.message"),
    )

    assert result["status"] == "PERMISSION_DENIED"


def test_add_or_update_cart_item_with_permission_returns_pending_confirmation():
    service = BiblioBotToolService()

    result = service.add_or_update_cart_item(
        AddOrUpdateCartItemInput(session_id="session-1", book_id="book-001", quantity=1),
        context_with("cart.manage"),
    )

    assert result["status"] == "PENDING_CONFIRMATION"
    assert result["requiresConfirmation"] is True
    assert result["pendingAction"]["mockOnly"] is True
    assert "DONE" not in result.values()


def test_create_sale_from_cart_with_permission_does_not_create_real_sale():
    service = BiblioBotToolService()

    result = service.create_sale_from_cart(
        CreateSaleFromCartInput(session_id="session-1", branch_id="branch-north"),
        context_with("sales.create"),
    )

    assert result["status"] == "PENDING_CONFIRMATION"
    assert result["pendingAction"]["details"]["origin_code"] == "CHATBOT"
    assert result["pendingAction"]["details"]["draft"]["mockOnly"] is True


def test_confirm_sale_with_permission_does_not_confirm_real_sale():
    service = BiblioBotToolService()

    result = service.confirm_sale(ConfirmSaleInput(sale_id="sale-001"), context_with("sales.confirm"))

    assert result["status"] == "PENDING_CONFIRMATION"
    assert result["requiresConfirmation"] is True
    assert "invoice" not in result


def test_register_inventory_entry_requires_confirmation_and_no_real_register():
    service = BiblioBotToolService()

    result = service.register_inventory_entry(
        RegisterInventoryEntryInput(book_id="book-001", branch_id="branch-north", quantity=3),
        context_with("inventory.entry"),
    )

    assert result["status"] == "PENDING_CONFIRMATION"
    assert result["pendingAction"]["intent"] == "inventory_entry"
    assert result["pendingAction"]["details"]["simulation"]["mockOnly"] is True


def test_create_purchase_request_requires_confirmation_and_no_real_create():
    service = BiblioBotToolService()

    result = service.create_purchase_request(
        CreatePurchaseRequestInput(branch_id="branch-north", book_id="book-001", quantity=2),
        context_with("requests.purchase.create"),
    )

    assert result["status"] == "PENDING_CONFIRMATION"
    assert result["pendingAction"]["intent"] == "purchase_request"
    assert result["pendingAction"]["details"]["simulation"]["mockOnly"] is True


def test_create_transfer_request_requires_confirmation_and_no_real_create():
    service = BiblioBotToolService()

    result = service.create_transfer_request(
        CreateTransferRequestInput(
            source_branch_id="branch-north",
            destination_branch_id="branch-south",
            book_id="book-001",
            quantity=1,
        ),
        context_with("requests.transfer.create"),
    )

    assert result["status"] == "PENDING_CONFIRMATION"
    assert result["pendingAction"]["intent"] == "transfer_request"
    assert result["pendingAction"]["details"]["simulation"]["mockOnly"] is True


def test_sensitive_tools_never_return_done_or_executing_action_or_real_invoice():
    service = BiblioBotToolService()
    sensitive_results = [
        service.add_or_update_cart_item(
            AddOrUpdateCartItemInput(session_id="session-1", book_id="book-001", quantity=1),
            context_with("cart.manage"),
        ),
        service.create_sale_from_cart(
            CreateSaleFromCartInput(session_id="session-1", branch_id="branch-north"),
            context_with("sales.create"),
        ),
        service.confirm_sale(ConfirmSaleInput(sale_id="sale-001"), context_with("sales.confirm")),
        service.register_inventory_entry(
            RegisterInventoryEntryInput(book_id="book-001", branch_id="branch-north", quantity=1),
            context_with("inventory.entry"),
        ),
        service.create_purchase_request(
            CreatePurchaseRequestInput(branch_id="branch-north", book_id="book-001", quantity=1),
            context_with("requests.purchase.create"),
        ),
        service.create_transfer_request(
            CreateTransferRequestInput(
                source_branch_id="branch-north",
                destination_branch_id="branch-south",
                book_id="book-001",
                quantity=1,
            ),
            context_with("requests.transfer.create"),
        ),
    ]

    for result in sensitive_results:
        assert result["status"] == "PENDING_CONFIRMATION"
        assert result["status"] not in {"DONE", "EXECUTING_ACTION"}
        assert "invoice" not in result


def test_langchain_tool_registry_returns_non_empty_unique_names():
    tools = get_langchain_tools()
    names = [tool.name for tool in tools]

    assert tools
    assert len(names) == len(set(names))


def test_langchain_tool_returns_json_serializable_value():
    tool = next(tool for tool in get_langchain_tools() if tool.name == "search_books")

    result = tool.invoke(
        {
            "input_data": {"query": "python"},
            "context": context_with("books.search").model_dump(),
        }
    )

    assert result["status"] == "MOCK_ONLY"
    assert_json_serializable(result)


def test_tools_do_not_use_openai_or_real_http_clients():
    tools_dir = Path(__file__).resolve().parents[1] / "app" / "tools"
    source = "\n".join(path.read_text(encoding="utf-8").lower() for path in tools_dir.rglob("*.py"))

    assert "openai" not in source
    assert "import httpx" not in source
    assert "from httpx" not in source
    assert "import requests" not in source
    assert "from requests" not in source


def test_tools_do_not_require_gemini():
    tools_dir = Path(__file__).resolve().parents[1] / "app" / "tools"
    source = "\n".join(path.read_text(encoding="utf-8").lower() for path in tools_dir.rglob("*.py"))

    assert "gemini" not in source
    assert "llm" not in source


def test_health_still_works_with_tools_present():
    response = client.get("/health")

    assert response.status_code == 200
    assert response.json()["status"] == "ok"


def test_chat_process_still_works_with_tools_present():
    response = client.post(
        "/chat/process",
        json={
            "sessionId": "session-123",
            "message": "Busco un libro de Python",
            "userId": "0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
            "roles": ["CLIENT"],
            "permissions": ["chat.message", "books.search"],
        },
    )

    assert response.status_code == 200
    assert response.json()["context"]["intent"] == "catalog_search"


def test_permission_service_still_blocks_admin_without_explicit_permission():
    service = PermissionService()

    assert service.can_access_intent("sales_query", []) is False


def test_confirmation_without_pending_action_still_does_not_execute():
    response = client.post(
        "/chat/process",
        json={
            "sessionId": "session-123",
            "message": "si confirmo",
            "userId": "0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
            "roles": ["CLIENT"],
            "permissions": ["chat.message"],
        },
    )

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["context"]["intent"] == "confirmation_without_pending_action"
