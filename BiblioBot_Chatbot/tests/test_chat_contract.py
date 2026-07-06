from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


def test_chat_process_accepts_dotnet_contract():
    payload = {
        "sessionId": "session-123",
        "message": "Quiero buscar libros de arquitectura",
        "userId": "0d4f3d2a-8f6a-4d50-86df-f1f80993b8e9",
        "userEmail": "cliente@example.com",
        "roles": ["CLIENT"],
        "permissions": ["books.read", "books.search", "chat.message"],
        "source": "DOTNET_BACKEND",
        "sentAt": "2026-07-06T12:00:00Z",
    }

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert set(body.keys()) == {"response", "state", "links", "uiAction", "context"}
    assert body["state"] == "INTENT_DETECTED"
    assert body["links"] == []
    assert body["uiAction"] == "NONE"
    assert body["context"]["intent"] == "contract_validation"
    assert body["context"]["requiresConfirmation"] is False
    assert body["context"]["saleOrigin"] == "CHATBOT"
    assert body["context"]["metadata"]["sessionId"] == payload["sessionId"]


def test_chat_process_rejects_invalid_dotnet_contract():
    payload = {
        "sessionId": "",
        "message": "",
        "userId": "not-a-uuid",
        "roles": ["CLIENT"],
        "permissions": ["chat.message"],
    }

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 422


def test_chat_process_response_keeps_future_fields_inside_context():
    payload = {
        "sessionId": "session-456",
        "message": "Necesito confirmar una compra",
        "userId": "26f79d05-a18a-4a3a-94c0-e581e9ba1d3b",
        "roles": ["CLIENT"],
        "permissions": ["cart.manage", "sales.create", "chat.message"],
    }

    response = client.post("/chat/process", json=payload)

    assert response.status_code == 200
    body = response.json()
    assert "requiresConfirmation" not in body
    assert "actionRef" not in body
    assert "invoiceNumber" not in body
    assert "nextAction" not in body
    assert "requiresConfirmation" in body["context"]
    assert "nextAction" in body["context"]
