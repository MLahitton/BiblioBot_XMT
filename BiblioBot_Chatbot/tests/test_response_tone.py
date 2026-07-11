from fastapi.testclient import TestClient

from app.main import app
from app.services.response_composer_service import ResponseComposerService


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


def test_response_variants_are_not_always_identical():
    service = ResponseComposerService()
    responses = {
        service.compose(
            {
                "session_id": f"variant-session-{index}",
                "message": f"recomiendame fantasia {index}",
                "intent": "catalog_search",
                "metadata": {
                    "books": [
                        {"title": "El Hobbit", "author": "J. R. R. Tolkien"},
                        {"title": "Matilda", "author": "Roald Dahl"},
                    ],
                    "resultCount": 2,
                },
            }
        )
        for index in range(8)
    }

    assert len(responses) > 1


def test_book_detail_uses_real_data():
    body = post_chat(
        "dame resumen de Matilda",
        permissions=["chat.message", "books.read", "books.search"],
    )

    text = normalized_response(body)
    assert body["context"]["intent"] == "book_detail"
    assert "matilda" in text
    assert "roald dahl" in text
    assert body["context"]["metadata"]["book"]["title"] == "Matilda"


def test_summary_does_not_invent_when_no_description():
    service = ResponseComposerService()
    response = service.compose(
        {
            "session_id": "summary-no-description",
            "message": "resumen de libro",
            "intent": "book_detail",
            "metadata": {
                "summaryRequested": True,
                "book": {
                    "title": "Libro Sin Sinopsis",
                    "author": "Autora Registrada",
                    "genre": "Ensayo",
                    "price": 12000,
                    "available": True,
                },
            },
            "tool_result": {"book": {"title": "Libro Sin Sinopsis", "author": "Autora Registrada"}},
        }
    ).lower()

    assert "libro sin sinopsis" in response
    assert "autora registrada" in response
    assert (
        "no tengo una sinopsis" in response
        or "no aparece una descripcion" in response
        or "no agrego trama" in response
    )


def test_out_of_domain_message_is_blocked():
    body = post_chat(
        "hazme una receta de pasta",
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["state"] == "NEEDS_CLARIFICATION"
    assert body["uiAction"] == "NONE"
    assert body["links"] == []
    assert body["context"]["intent"] == "out_of_domain"
    assert body["context"]["nextAction"] == "OUT_OF_DOMAIN"
    assert body["context"]["metadata"]["books"] == []
    assert "bibliobot" in normalized_response(body)


def test_out_of_domain_varies_response():
    service = ResponseComposerService()
    responses = {
        service.compose(
            {
                "session_id": f"ood-session-{index}",
                "message": f"hazme una receta de pasta {index}",
                "intent": "out_of_domain",
                "metadata": {},
            }
        )
        for index in range(8)
    }

    assert len(responses) > 1


def test_recommendation_not_auth_required():
    body = post_chat(
        "recomiendame libros de fantasia",
        userId=None,
        userEmail=None,
        roles=["GUEST"],
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["context"]["intent"] == "catalog_search"
    assert body["context"]["nextAction"] == "SEARCH_BOOKS_PENDING"
    assert body["context"]["nextAction"] != "AUTH_REQUIRED"


def test_list_categories_not_general_help():
    body = post_chat(
        "que categorias hay",
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["context"]["intent"] == "list_categories"
    assert body["context"]["nextAction"] == "CATEGORIES_READY"
    assert "categorias" in normalized_response(body)


def test_catalog_response_has_books_when_results_exist():
    body = post_chat(
        "recomiendame libros de fantasia",
        permissions=["chat.message", "books.read", "books.search"],
    )

    books = body["context"]["metadata"]["books"]
    assert body["context"]["intent"] == "catalog_search"
    assert body["context"]["metadata"]["resultCount"] > 0
    assert books
    assert any(book["title"].lower() in normalized_response(body) for book in books)


def test_checkout_cart_regression_still_requires_auth_for_guest():
    body = post_chat(
        "finalizar compra",
        userId=None,
        userEmail=None,
        roles=["GUEST"],
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["context"]["intent"] == "auth_required"
    assert body["context"]["nextAction"] == "AUTH_REQUIRED"
    assert "iniciar sesion" in normalized_response(body)


def test_confirm_sale_regression_still_requires_permission():
    body = post_chat(
        "confirmar venta sale-001",
        roles=["ADMIN"],
        permissions=["chat.message"],
    )

    assert body["state"] == "FAILED"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"
    assert "permiso necesario" in normalized_response(body)


def test_stock_regression_keeps_inventory_result():
    body = post_chat(
        "hay stock de Python Practico",
        permissions=["chat.message", "books.read", "inventory.read"],
    )

    assert body["context"]["intent"] == "stock_check"
    assert body["context"]["nextAction"] == "STOCK_CHECK_READY"
    assert "python practico" in normalized_response(body)


def test_typo_hoal_is_greeting():
    body = post_chat("hoal")

    assert body["context"]["intent"] == "greeting"
    assert body["context"]["intent"] != "out_of_domain"
    assert "bibliobot" in normalized_response(body)


def test_holaa_is_greeting():
    body = post_chat("holaa")

    assert body["context"]["intent"] == "greeting"
    assert body["context"]["nextAction"] == "WAITING_USER_MESSAGE"


def test_hola_quien_eres_identity():
    body = post_chat("hola quien eres")

    assert body["context"]["intent"] == "identity_help"
    assert "bibliobot" in normalized_response(body)
    assert len(body["response"]) < 320


def test_quien_eres_identity():
    body = post_chat("quien eres")

    assert body["context"]["intent"] == "identity_help"
    assert body["context"]["intent"] != "out_of_domain"
    assert "bibliobot" in normalized_response(body)


def test_que_puedes_hacer_help():
    body = post_chat("que puedes hacer")

    text = normalized_response(body)
    assert body["context"]["intent"] == "identity_help"
    assert "libro" in text or "catalogo" in text
    assert len(body["response"]) < 320


def test_politics_out_of_domain():
    body = post_chat("hablame de politica")

    assert body["context"]["intent"] == "out_of_domain"
    assert body["context"]["nextAction"] == "OUT_OF_DOMAIN"
    assert body["uiAction"] == "NONE"


def test_contextual_stock_uses_last_book():
    session_id = "tone-context-stock-anna"
    detail = post_chat(
        "de que trata el libro anna karenina",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search"],
    )
    stock = post_chat(
        "cuantos hay disponibles",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert detail["context"]["intent"] == "book_detail"
    assert stock["context"]["intent"] == "stock_context_query"
    assert stock["context"]["nextAction"] == "STOCK_CHECK_READY"
    assert "anna karenina" in normalized_response(stock)
    assert "9" in normalized_response(stock)


def test_explicit_stock_query_anna_karenina():
    body = post_chat(
        "cuantos libros de anna karenina",
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["context"]["intent"] == "stock_explicit_query"
    assert body["context"]["nextAction"] == "STOCK_CHECK_READY"
    assert "anna karenina" in normalized_response(body)


def test_admin_inventory_adjustment_detected():
    body = post_chat(
        "quiero que saques uno del stock y que queden 9 libros de anna karenina",
        roles=["ADMIN"],
        permissions=["chat.message", "inventory.entry", "books.read", "books.search"],
    )

    metadata = body["context"]["metadata"]
    assert body["context"]["intent"] == "admin_inventory_adjustment"
    assert body["uiAction"] == "NAVIGATE_TO_INVENTORY_ADJUSTMENT"
    assert body["context"]["nextAction"] == "NAVIGATE_TO_INVENTORY_ADJUSTMENT"
    assert metadata["bookTitle"] == "Anna Karenina"
    assert metadata["adjustmentType"] == "OUT"
    assert metadata["quantity"] == 1
    assert metadata["expectedStockAfter"] == 9
    assert metadata["safeMutationAvailable"] is False
    assert "quedo cambiado" not in normalized_response(body)
    assert "actualice" not in normalized_response(body)


def test_guest_inventory_adjustment_blocked():
    body = post_chat(
        "quiero que saques uno del stock de anna karenina",
        userId=None,
        userEmail=None,
        roles=["GUEST"],
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["context"]["intent"] == "auth_required"
    assert body["context"]["metadata"]["originalIntent"] == "admin_inventory_adjustment"
    assert body["context"]["nextAction"] == "AUTH_REQUIRED"


def test_client_inventory_adjustment_denied():
    body = post_chat(
        "quiero que saques uno del stock de anna karenina",
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "admin_inventory_adjustment"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_admin_create_user_navigation():
    body = post_chat(
        "quiero agregar un usuario",
        roles=["ADMIN"],
        permissions=["chat.message", "admin.users.read"],
    )

    assert body["context"]["intent"] == "admin_navigation"
    assert body["uiAction"] == "NAVIGATE_TO_ADMIN_CREATE_USER"
    assert body["context"]["metadata"]["frontendRoute"] == "/admin/usuarios"


def test_guest_create_user_blocked():
    body = post_chat(
        "quiero agregar un usuario",
        userId=None,
        userEmail=None,
        roles=["GUEST"],
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["context"]["intent"] == "auth_required"
    assert body["context"]["metadata"]["originalIntent"] == "admin_navigation"
    assert body["context"]["nextAction"] == "AUTH_REQUIRED"


def test_client_create_user_denied():
    body = post_chat(
        "quiero agregar un usuario",
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["state"] == "FAILED"
    assert body["context"]["intent"] == "admin_navigation"
    assert body["context"]["nextAction"] == "PERMISSION_DENIED"


def test_purchase_auth_regression_still_waits_confirmation():
    body = post_chat(
        "quiero comprar Matilda",
        permissions=["chat.message", "cart.manage", "books.read", "books.search"],
    )

    assert body["context"]["intent"] == "purchase_intent"
    assert body["state"] == "WAITING_CONFIRMATION"


def test_checkout_cart_authenticated_regression():
    body = post_chat(
        "finalizar compra",
        permissions=["chat.message", "sales.create"],
    )

    assert body["context"]["intent"] == "checkout_cart"
    assert body["state"] == "WAITING_CONFIRMATION"


def test_confirm_sale_authenticated_regression():
    body = post_chat(
        "confirmar venta",
        roles=["ADMIN"],
        permissions=["chat.message", "sales.confirm"],
    )

    assert body["context"]["intent"] == "confirm_sale"
    assert body["state"] == "WAITING_CONFIRMATION"


def test_halo_is_greeting():
    body = post_chat("halo")

    assert body["context"]["intent"] == "greeting"
    assert body["context"]["intent"] != "out_of_domain"


def test_hols_is_greeting():
    body = post_chat("hols")

    assert body["context"]["intent"] == "greeting"
    assert body["context"]["intent"] != "out_of_domain"


def test_list_categories_deduplicated():
    body = post_chat(
        "dime que categorias hay",
        permissions=["chat.message", "books.read", "books.search"],
    )
    categories = body["context"]["metadata"]["categories"]
    normalized = [category.lower() for category in categories]

    assert body["context"]["intent"] == "list_categories"
    assert categories
    assert len(normalized) == len(set(normalized))


def test_category_followup_de_misterio():
    session_id = "tone-category-followup-misterio"
    first = post_chat(
        "recomiendame un libro",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search"],
    )
    second = post_chat(
        "de misterio",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert first["context"]["intent"] == "catalog_search"
    assert second["context"]["intent"] == "refine_catalog_filter"
    assert second["context"]["metadata"]["filters"]["genre"] == "misterio"
    assert second["context"]["intent"] != "out_of_domain"
    assert second["context"]["metadata"]["books"]


def test_category_query_infantil():
    body = post_chat(
        "un libro de categoria infantil",
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["context"]["intent"] == "refine_catalog_filter"
    assert body["context"]["metadata"]["filters"]["genre"] == "infantil"
    assert body["context"]["metadata"]["books"]


def test_followup_more_recommendations_uses_last_catalog():
    session_id = "tone-more-recommendations-ciencia"
    first = post_chat(
        "libros de ciencia",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search"],
    )
    second = post_chat(
        "que mas me recomiendas",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert first["context"]["metadata"]["books"]
    assert second["context"]["intent"] == "catalog_search"
    assert second["context"]["nextAction"] == "BOOK_RECOMMENDATION_READY"
    assert "no encontre" not in normalized_response(second)
    assert "ciencia" in normalized_response(second)


def test_stock_context_after_summary_clean_architecture():
    session_id = "tone-stock-clean-architecture"
    summary = post_chat(
        "dame un resumen del libro Clean Architecture",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search"],
    )
    stock = post_chat(
        "cuanto hay disponibles",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert summary["context"]["intent"] == "book_detail"
    assert stock["context"]["intent"] == "stock_context_query"
    assert stock["context"]["nextAction"] == "STOCK_CHECK_READY"
    assert "clean architecture" in normalized_response(stock)
    assert "10" in normalized_response(stock)


def test_stock_context_after_explicit_stock_query():
    session_id = "tone-explicit-stock-then-purchase"
    stock = post_chat(
        "cuantos libros hay disponibles del libro Clean Architecture",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search", "cart.manage"],
    )
    purchase = post_chat(
        "quiero uno",
        sessionId=session_id,
        permissions=["chat.message", "books.read", "books.search", "cart.manage"],
    )

    assert stock["context"]["nextAction"] == "STOCK_CHECK_READY"
    assert "clean architecture" in normalized_response(stock)
    assert purchase["context"]["intent"] == "purchase_intent"
    assert purchase["state"] == "WAITING_CONFIRMATION"
    assert purchase["context"]["metadata"]["bookTitle"] == "Clean Architecture"
    assert purchase["context"]["metadata"]["quantity"] == 1


def test_guest_contextual_purchase_auth_required():
    session_id = "tone-guest-contextual-purchase"
    stock = post_chat(
        "cuantos libros hay disponibles del libro Clean Architecture",
        sessionId=session_id,
        userId=None,
        userEmail=None,
        roles=["GUEST"],
        permissions=["chat.message", "books.read", "books.search"],
    )
    purchase = post_chat(
        "quiero uno",
        sessionId=session_id,
        userId=None,
        userEmail=None,
        roles=["GUEST"],
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert stock["context"]["nextAction"] == "STOCK_CHECK_READY"
    assert purchase["context"]["intent"] == "auth_required"
    assert purchase["context"]["metadata"]["originalIntent"] == "purchase_intent"
    assert purchase["context"]["metadata"]["bookTitle"] == "Clean Architecture"


def test_out_of_domain_without_context_still_blocked_phase17():
    body = post_chat(
        "hazme una receta de pasta",
        sessionId="tone-out-of-domain-no-context",
        permissions=["chat.message", "books.read", "books.search"],
    )

    assert body["context"]["intent"] == "out_of_domain"
    assert body["context"]["nextAction"] == "OUT_OF_DOMAIN"
