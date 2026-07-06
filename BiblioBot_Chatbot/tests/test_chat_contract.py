from fastapi.testclient import TestClient

from app.main import app


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
    payload = build_payload("Busco un libro de Python")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["context"]["intent"] == "catalog_search"
    assert body["state"] == "INTENT_DETECTED"
    assert body["uiAction"] == "NAVIGATE_TO_CATALOG"


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


def test_purchase_intent_asks_for_book_and_quantity():
    payload = build_payload("Quiero comprar")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["context"]["intent"] == "purchase_intent"
    assert body["state"] == "ASKING_DETAILS"
    assert body["context"]["nextAction"] == "ASK_BOOK_AND_QUANTITY"
    assert body["context"]["requiresConfirmation"] is False


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
    payload = build_payload("muestrame la factura FAC-0001 del libro")

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert body["state"] == "ASKING_DETAILS"
    assert body["context"]["intent"] == "invoice_query"
    assert body["context"]["nextAction"] == "ASK_INVOICE_OR_SALE_ID"


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
