import json

import httpx
import pytest

from app.clients.dotnet_api_client import DotNetApiClient
from app.clients.dotnet_client_errors import (
    DotNetApiBadRequestError,
    DotNetApiConflictError,
    DotNetApiForbiddenError,
    DotNetApiInvalidResponseError,
    DotNetApiMutationDisabledError,
    DotNetApiNotFoundError,
    DotNetApiTimeoutError,
    DotNetApiUnauthorizedError,
    DotNetApiUnavailableError,
)
from app.core.config import Settings


def make_settings(**overrides) -> Settings:
    values = {
        "use_mock_dotnet_client": False,
        "dotnet_api_base_url": "http://dotnet.test",
        "dotnet_api_timeout_seconds": 5,
        "dotnet_api_bearer_token": None,
        "allow_real_backend_mutations": False,
    }
    values.update(overrides)
    return Settings(**values)


def make_client(handler, **settings_overrides) -> DotNetApiClient:
    return DotNetApiClient(make_settings(**settings_overrides), transport=httpx.MockTransport(handler))


def json_response(data, status_code=200) -> httpx.Response:
    return httpx.Response(status_code, json=data)


def test_search_books_with_query_uses_search_endpoint():
    seen = {}

    def handler(request: httpx.Request) -> httpx.Response:
        seen["path"] = request.url.path
        seen["query"] = dict(request.url.params)
        return json_response({"items": [{"id": "111", "title": "Python Practico", "totalStock": 3}]})

    result = make_client(handler).search_books("python")

    assert seen["path"] == "/api/libros/search"
    assert seen["query"]["q"] == "python"
    assert result[0]["title"] == "Python Practico"
    assert result[0]["available"] is True


def test_search_books_without_query_uses_catalog_endpoint():
    seen = {}

    def handler(request: httpx.Request) -> httpx.Response:
        seen["path"] = request.url.path
        return json_response([{"id": "222", "titulo": "Clean Code", "stock": 1}])

    result = make_client(handler).search_books()

    assert seen["path"] == "/api/libros"
    assert result[0]["id"] == "222"
    assert result[0]["title"] == "Clean Code"


def test_search_books_accepts_paged_items_shape():
    def handler(request: httpx.Request) -> httpx.Response:
        return json_response({"items": [{"id": "333", "titulo": "Arquitectura Limpia"}], "totalCount": 1})

    result = make_client(handler).search_books("arquitectura")

    assert len(result) == 1
    assert result[0]["title"] == "Arquitectura Limpia"


def test_search_books_maps_real_backend_authors_and_categories():
    def handler(request: httpx.Request) -> httpx.Response:
        return json_response(
            {
                "items": [
                    {
                        "id": "444",
                        "title": "El Hobbit",
                        "authors": ["J. R. R. Tolkien"],
                        "categories": ["Fantasía", "Aventura"],
                        "totalStock": 10,
                    }
                ],
                "totalCount": 1,
            }
        )

    result = make_client(handler).search_books("fantasia")

    assert result[0]["author"] == "J. R. R. Tolkien"
    assert result[0]["genre"] == "Fantasía, Aventura"
    assert result[0]["authors"] == ["J. R. R. Tolkien"]
    assert result[0]["categories"] == ["Fantasía", "Aventura"]


def test_get_book_detail_uses_book_id_path():
    seen = {}

    def handler(request: httpx.Request) -> httpx.Response:
        seen["path"] = request.url.path
        return json_response({"id": "book-guid", "title": "DDD", "totalStock": 4})

    result = make_client(handler).get_book_detail("book-guid")

    assert seen["path"] == "/api/libros/book-guid"
    assert result["id"] == "book-guid"


def test_check_stock_uses_total_stock_from_book_detail():
    def handler(request: httpx.Request) -> httpx.Response:
        return json_response({"id": "book-guid", "title": "DDD", "totalStock": 4})

    result = make_client(handler).check_stock("book-guid")

    assert result["bookId"] == "book-guid"
    assert result["totalStock"] == 4
    assert result["available"] is True


@pytest.mark.parametrize(
    ("status_code", "expected_error"),
    [
        (400, DotNetApiBadRequestError),
        (401, DotNetApiUnauthorizedError),
        (403, DotNetApiForbiddenError),
        (404, DotNetApiNotFoundError),
        (409, DotNetApiConflictError),
        (500, DotNetApiUnavailableError),
    ],
)
def test_http_errors_are_mapped(status_code, expected_error):
    def handler(request: httpx.Request) -> httpx.Response:
        return json_response({"error": "hidden"}, status_code=status_code)

    client = make_client(handler)

    with pytest.raises(expected_error):
        client.search_books("python")


def test_timeout_is_mapped_safely():
    def handler(request: httpx.Request) -> httpx.Response:
        raise httpx.TimeoutException("timeout", request=request)

    with pytest.raises(DotNetApiTimeoutError):
        make_client(handler).search_books("python")


def test_invalid_json_is_mapped_safely():
    def handler(request: httpx.Request) -> httpx.Response:
        return httpx.Response(200, content=b"<html>not json</html>")

    with pytest.raises(DotNetApiInvalidResponseError):
        make_client(handler).search_books("python")


def test_authorization_header_only_when_token_is_configured():
    headers = []

    def handler(request: httpx.Request) -> httpx.Response:
        headers.append(request.headers)
        return json_response([])

    make_client(handler).search_books()
    make_client(handler, dotnet_api_bearer_token="service-token").search_books()

    assert "Authorization" not in headers[0]
    assert headers[1]["Authorization"] == "Bearer service-token"
    assert "service-token" not in repr(make_client(handler, dotnet_api_bearer_token="service-token").settings)


def test_arbitrary_paths_are_rejected_before_request():
    called = False

    def handler(request: httpx.Request) -> httpx.Response:
        nonlocal called
        called = True
        return json_response({})

    client = make_client(handler)

    with pytest.raises(Exception):
        client._request_json("GET", "http://evil.test/api")
    assert called is False


def test_dynamic_path_segments_are_encoded_and_cannot_inject_query_or_slashes():
    seen = {}

    def handler(request: httpx.Request) -> httpx.Response:
        seen["raw_path"] = request.url.raw_path
        seen["query"] = request.url.query
        return json_response({"id": "book", "title": "Seguro"})

    make_client(handler).get_book_detail("book/../evil?x=1")

    assert seen["raw_path"] == b"/api/libros/book%2F..%2Fevil%3Fx%3D1"
    assert seen["query"] == b""


def test_client_creation_does_not_make_requests():
    calls = 0

    def handler(request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        return json_response([])

    DotNetApiClient(make_settings(), transport=httpx.MockTransport(handler))

    assert calls == 0


@pytest.mark.parametrize(
    ("method_name", "args"),
    [
        ("add_or_update_cart_item", ("s1", "b1", 1)),
        ("create_sale_from_cart", ("s1",)),
        ("confirm_sale", ("sale-1",)),
        ("register_inventory_entry", ("b1", 1, "branch-1")),
        ("create_purchase_request", ("branch-1", "b1", 1)),
        ("create_transfer_request", ("branch-1", "branch-2", "b1", 1)),
    ],
)
def test_mutations_are_disabled_by_default(method_name, args):
    calls = 0

    def handler(request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        return json_response({})

    client = make_client(handler)

    with pytest.raises(DotNetApiMutationDisabledError):
        getattr(client, method_name)(*args)
    assert calls == 0


def test_no_openai_or_database_imports_in_client_source():
    source = "\n".join(
        [
            DotNetApiClient.__module__,
            json.dumps(DotNetApiClient.__dict__, default=str).lower(),
        ]
    )

    assert "openai" not in source
    assert "psycopg" not in source
    assert "pgvector" not in source
