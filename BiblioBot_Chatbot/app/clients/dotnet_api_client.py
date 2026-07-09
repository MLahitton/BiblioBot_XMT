from typing import Any
from urllib.parse import quote

import httpx

from app.clients.dotnet_client_errors import (
    DotNetApiBadRequestError,
    DotNetApiConflictError,
    DotNetApiError,
    DotNetApiForbiddenError,
    DotNetApiInvalidResponseError,
    DotNetApiMutationDisabledError,
    DotNetApiNotFoundError,
    DotNetApiTimeoutError,
    DotNetApiUnauthorizedError,
    DotNetApiUnavailableError,
)
from app.core.config import Settings, settings


class DotNetApiClient:
    def __init__(
        self,
        app_settings: Settings | None = None,
        transport: httpx.BaseTransport | None = None,
    ):
        self.settings = app_settings or settings
        self.base_url = self._validate_base_url(self.settings.dotnet_api_base_url)
        self.timeout = httpx.Timeout(self.settings.dotnet_api_timeout_seconds)
        self.transport = transport

    def search_books(self, query: str | None = None, page_size: int = 10) -> list[dict]:
        params = {"pageNumber": 1, "pageSize": page_size}
        path = "/api/libros"
        if query and len(query.strip()) >= 2:
            path = "/api/libros/search"
            params["q"] = query.strip()
        data = self._request_json("GET", path, params=params)
        return [self._normalize_book(item) for item in self._extract_items(data)]

    def get_book_detail(self, book_id: str) -> dict | None:
        try:
            data = self._request_json("GET", self._build_path("api", "libros", book_id))
        except DotNetApiNotFoundError:
            return None
        return self._normalize_book(data)

    def check_stock(self, book_id: str, branch_id: str | None = None) -> dict | None:
        book = self.get_book_detail(book_id)
        if not book:
            return None
        total_stock = book.get("totalStock")
        if branch_id:
            return {
                "bookId": book["id"],
                "title": book["title"],
                "branchId": branch_id,
                "stock": None,
                "available": None,
                "status": "REAL_BACKEND_LIMITED",
                "message": "El stock por sede requiere endpoint protegido o datos adicionales del backend.",
            }
        return {
            "bookId": book["id"],
            "title": book["title"],
            "totalStock": total_stock,
            "available": book.get("available"),
            "status": "REAL_BACKEND",
        }

    def get_cart(self, session_id: str) -> dict:
        return self._request_json("GET", self._build_path("api", "carrito", session_id))

    def add_or_update_cart_item(
        self,
        session_id: str,
        book_id: str,
        quantity: int,
        branch_id: str | None = None,
    ) -> dict:
        self._ensure_mutations_allowed()
        return self._request_json(
            "POST",
            "/api/carrito",
            json={"sessionId": session_id, "bookId": book_id, "quantity": quantity, "branchId": branch_id},
        )

    def create_sale_from_cart(
        self,
        session_id: str,
        branch_id: str | None = None,
        origin_code: str = "CHATBOT",
    ) -> dict:
        self._ensure_mutations_allowed()
        return self._request_json(
            "POST",
            "/api/ventas",
            json={"sessionId": session_id, "branchId": branch_id, "originCode": origin_code},
        )

    def confirm_sale(self, sale_id: str) -> dict:
        self._ensure_mutations_allowed()
        return self._request_json("POST", self._build_path("api", "ventas", sale_id, "confirmar"))

    def get_invoice(self, invoice_id: str | None = None, sale_id: str | None = None) -> dict | None:
        if not invoice_id and not sale_id:
            raise DotNetApiBadRequestError("Se requiere invoice_id o sale_id para consultar factura.")
        path = (
            self._build_path("api", "facturas", invoice_id)
            if invoice_id
            else self._build_path("api", "facturas", "venta", sale_id)
        )
        try:
            return self._request_json("GET", path)
        except DotNetApiNotFoundError:
            return None

    def query_sales(self, scope: str = "own") -> list[dict]:
        data = self._request_json("GET", "/api/ventas")
        sales = self._extract_items(data)
        return [{**sale, "scope": scope} for sale in sales]

    def query_inventory(self, branch_id: str | None = None) -> list[dict]:
        params = {"branchId": branch_id} if branch_id else None
        data = self._request_json("GET", "/api/inventario", params=params)
        return self._extract_items(data)

    def register_inventory_entry(
        self,
        book_id: str,
        quantity: int,
        branch_id: str,
        reason: str | None = None,
    ) -> dict:
        self._ensure_mutations_allowed()
        return self._request_json(
            "POST",
            "/api/inventario/entradas",
            json={"bookId": book_id, "quantity": quantity, "branchId": branch_id, "reason": reason},
        )

    def create_purchase_request(
        self,
        branch_id: str,
        book_id: str,
        quantity: int,
        notes: str | None = None,
    ) -> dict:
        self._ensure_mutations_allowed()
        return self._request_json(
            "POST",
            "/api/solicitudes/compras",
            json={"branchId": branch_id, "bookId": book_id, "quantity": quantity, "notes": notes},
        )

    def create_transfer_request(
        self,
        source_branch_id: str,
        destination_branch_id: str,
        book_id: str,
        quantity: int,
        notes: str | None = None,
    ) -> dict:
        self._ensure_mutations_allowed()
        return self._request_json(
            "POST",
            "/api/solicitudes/traslados",
            json={
                "sourceBranchId": source_branch_id,
                "destinationBranchId": destination_branch_id,
                "bookId": book_id,
                "quantity": quantity,
                "notes": notes,
            },
        )

    def get_low_stock_books(self) -> list[dict]:
        return [item for item in self.search_books() if (item.get("totalStock") or 0) <= 2 and (item.get("totalStock") or 0) > 0]

    def list_branches(self) -> list[dict]:
        return []

    def create_sale_draft(
        self,
        session_id: str,
        book_id: str | None = None,
        quantity: int | None = None,
        branch_id: str | None = None,
    ) -> dict:
        return {
            "sessionId": session_id,
            "bookId": book_id,
            "quantity": quantity,
            "branchId": branch_id,
            "status": "PENDING_CONFIRMATION",
            "realBackendMutationBlocked": True,
        }

    def simulate_inventory_entry(
        self,
        book_id: str,
        quantity: int,
        branch_id: str | None = None,
    ) -> dict:
        return self._blocked_simulation({"bookId": book_id, "quantity": quantity, "branchId": branch_id})

    def simulate_transfer_request(
        self,
        book_id: str,
        quantity: int,
        from_branch_id: str | None = None,
        to_branch_id: str | None = None,
    ) -> dict:
        return self._blocked_simulation(
            {"bookId": book_id, "quantity": quantity, "fromBranchId": from_branch_id, "toBranchId": to_branch_id}
        )

    def simulate_purchase_request(
        self,
        book_id: str | None = None,
        quantity: int | None = None,
        reason: str | None = None,
    ) -> dict:
        return self._blocked_simulation({"bookId": book_id, "quantity": quantity, "reason": reason})

    def _request_json(
        self,
        method: str,
        path: str,
        params: dict[str, Any] | None = None,
        json: dict[str, Any] | None = None,
    ) -> Any:
        self._validate_path(path)
        try:
            with httpx.Client(
                base_url=self.base_url,
                timeout=self.timeout,
                headers=self._headers(),
                transport=self.transport,
            ) as client:
                response = client.request(method, path, params=params, json=json)
            self._raise_for_status(response)
            return response.json()
        except httpx.TimeoutException as exc:
            raise DotNetApiTimeoutError("Tiempo de espera agotado al consultar ASP.NET Core.") from exc
        except httpx.ConnectError as exc:
            raise DotNetApiUnavailableError("ASP.NET Core no esta disponible.") from exc
        except httpx.RequestError as exc:
            raise DotNetApiUnavailableError("No se pudo consultar ASP.NET Core.") from exc
        except ValueError as exc:
            raise DotNetApiInvalidResponseError("ASP.NET Core devolvio una respuesta JSON invalida.") from exc

    def _headers(self) -> dict[str, str]:
        headers = {"Accept": "application/json"}
        token = self.settings.dotnet_api_bearer_token
        if token:
            headers["Authorization"] = f"Bearer {token}"
        return headers

    def _raise_for_status(self, response: httpx.Response) -> None:
        if response.status_code < 400:
            return
        errors: dict[int, type[DotNetApiError]] = {
            400: DotNetApiBadRequestError,
            401: DotNetApiUnauthorizedError,
            403: DotNetApiForbiddenError,
            404: DotNetApiNotFoundError,
            409: DotNetApiConflictError,
        }
        error_class = errors.get(response.status_code)
        if error_class:
            raise error_class("ASP.NET Core rechazo la solicitud de forma controlada.")
        if response.status_code >= 500:
            raise DotNetApiUnavailableError("ASP.NET Core no esta disponible temporalmente.")
        raise DotNetApiError("ASP.NET Core rechazo la solicitud.")

    def _extract_items(self, data: Any) -> list[dict]:
        if isinstance(data, list):
            return [item for item in data if isinstance(item, dict)]
        if not isinstance(data, dict):
            raise DotNetApiInvalidResponseError("La respuesta del backend no tiene el formato esperado.")
        for key in ("items", "data", "results", "libros", "books"):
            value = data.get(key)
            if isinstance(value, list):
                return [item for item in value if isinstance(item, dict)]
        return [data]

    def _normalize_book(self, item: dict) -> dict:
        title = item.get("title") or item.get("titulo") or item.get("name") or item.get("nombre") or ""
        author = item.get("author") or item.get("autor") or item.get("authorName") or ""
        genre = item.get("genre") or item.get("genero") or item.get("category") or item.get("categoria") or ""
        total_stock = item.get("totalStock", item.get("stockTotal", item.get("stock")))
        available = item.get("available", item.get("disponible"))
        if available is None and total_stock is not None:
            available = total_stock > 0
        return {
            **item,
            "id": str(item.get("id") or item.get("bookId") or item.get("libroId") or ""),
            "title": title,
            "author": author,
            "genre": genre,
            "description": item.get("description") or item.get("descripcion") or "",
            "price": item.get("price", item.get("precio")),
            "available": bool(available),
            "totalStock": total_stock,
        }

    def _ensure_mutations_allowed(self) -> None:
        if not self.settings.allow_real_backend_mutations:
            raise DotNetApiMutationDisabledError()

    def _validate_base_url(self, value: str) -> str:
        if not value or not value.strip():
            raise DotNetApiError("DOTNET_API_BASE_URL es requerido para el cliente real.")
        parsed = httpx.URL(value)
        if parsed.scheme not in {"http", "https"} or not parsed.host:
            raise DotNetApiError("DOTNET_API_BASE_URL no es una URL http/https valida.")
        return str(parsed)

    def _build_path(self, *segments: str | None) -> str:
        safe_segments = [quote(str(segment), safe="") for segment in segments if segment is not None]
        return "/" + "/".join(safe_segments)

    def _validate_path(self, path: str) -> None:
        if (
            not path.startswith("/")
            or "\\" in path
            or "://" in path
            or path.startswith("//")
            or "?" in path
            or "#" in path
        ):
            raise DotNetApiError("Ruta interna invalida para ASP.NET Core.")

    def _blocked_simulation(self, payload: dict) -> dict:
        return {
            **payload,
            "status": "PENDING_CONFIRMATION",
            "realBackendMutationBlocked": True,
        }
