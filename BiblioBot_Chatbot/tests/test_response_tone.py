from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


def payload(message: str, **overrides):
    data = {
        "sessionId": "tone-session-001",
        "message": message,
        "userId": "0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        "userEmail": "cliente@example.com",
        "roles": ["CLIENT"],
        "permissions": ["chat.message"],
        "source": "DOTNET_BACKEND",
    }
    data.update(overrides)
    return data


def post_chat(message: str, **overrides):
    response = client.post("/chat/process", json=payload(message, **overrides))
    assert response.status_code == 200
    return response.json()


def normalized_response(body: dict) -> str:
    return body["response"].lower()


def assert_no_completed_action_claim(body: dict):
    text = normalized_response(body)
    forbidden = [
        "compra realizada",
        "accion realizada",
        "inventario registrado",
        "factura generada",
        "ya lo hice",
        "venta confirmada",
    ]
    assert all(phrase not in text for phrase in forbidden)


def test_auth_required_tone_keeps_contract_and_links():
    body = post_chat(
        "Quiero comprar 2 Python Practico",
        userId=None,
        userEmail=None,
        roles=["GUEST"],
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["uiAction"] == "NONE"
    assert body["context"]["nextAction"] == "AUTH_REQUIRED"
    assert "iniciar sesion" in normalized_response(body)
    assert {link["url"] for link in body["links"]} == {"/auth/login", "/auth/register"}
    assert body["context"]["metadata"].get("pendingAction") is None


def test_admin_without_explicit_permission_is_denied_by_permission_not_role():
    body = post_chat(
        "muestrame ventas",
        roles=["ADMIN"],
        permissions=["chat.message"],
    )

    assert body["state"] == "FAILED"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"
    assert "permiso necesario" in normalized_response(body)
    assert "administrador" not in normalized_response(body)


def test_waiting_confirmation_does_not_claim_purchase_completed():
    body = post_chat(
        "Quiero comprar 2 Python Practico",
        permissions=["chat.message", "cart.manage"],
    )

    assert body["state"] == "WAITING_CONFIRMATION"
    assert body["context"]["requiresConfirmation"] is True
    assert "confirmes" in normalized_response(body)
    assert_no_completed_action_claim(body)


def test_inventory_pending_does_not_claim_inventory_registered():
    body = post_chat(
        "registrar entrada de 3 Python Practico en Sede Norte",
        roles=["ADMIN"],
        permissions=["chat.message", "inventory.entry", "books.read"],
    )

    assert body["state"] == "WAITING_CONFIRMATION"
    assert body["context"]["requiresConfirmation"] is True
    assert_no_completed_action_claim(body)


def test_response_shape_and_visual_routes_stay_unchanged():
    catalog = post_chat("recomiendame libros de fantasia", permissions=["chat.message", "books.search"])
    detail = post_chat("ver libro Python Practico", permissions=["chat.message", "books.read"])

    assert set(catalog.keys()) == {"response", "state", "links", "uiAction", "context"}
    assert "suggestions" not in catalog
    assert catalog["uiAction"] == "NAVIGATE_TO_CATALOG"
    assert catalog["links"][0]["url"].startswith("/search")
    assert detail["uiAction"] == "NAVIGATE_TO_PRODUCT"
    assert detail["links"][0]["url"].startswith("/books/")


def test_controlled_errors_do_not_expose_traceback_or_tokens():
    body = post_chat(
        "hola",
        sessionId="",
        permissions=["chat.message"],
    )

    text = normalized_response(body)
    assert body["state"] == "NEEDS_CLARIFICATION"
    assert "traceback" not in text
    assert "token" not in text
    assert "secret" not in text
