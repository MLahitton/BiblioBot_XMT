import re
import unicodedata
from urllib.parse import urlencode

from app.schemas.chat_contract import ChatLink


class FrontendActionService:
    SEARCH_ROUTE = "/search"
    CART_ROUTE = "/cart"
    LOGIN_ROUTE = "/auth/login"
    REGISTER_ROUTE = "/auth/register"

    INITIAL_SUGGESTIONS = [
        "Recomiendame ficcion",
        "Libros para aprender",
        "Algo para regalar",
        "Ver libros disponibles",
        "Buscar libros de fantasia",
        "Libros de programacion",
        "Libros para empezar a leer",
    ]

    def build_login_links(self) -> list[ChatLink]:
        return [
            ChatLink(label="Iniciar sesion", url=self.LOGIN_ROUTE, type="AUTH_LOGIN"),
            ChatLink(label="Crear cuenta", url=self.REGISTER_ROUTE, type="AUTH_REGISTER"),
        ]

    def build_catalog_metadata(self, query: str | None, filters: dict | None = None) -> dict:
        metadata = {"frontendRoute": self.SEARCH_ROUTE}
        if query:
            metadata["query"] = query
        if filters:
            metadata["filters"] = filters
            genre = filters.get("genre")
            if genre:
                metadata["genre"] = genre
            category = filters.get("category")
            if category:
                metadata["category"] = category
        return metadata

    def build_catalog_link(self, query: str | None, filters: dict | None = None) -> ChatLink | None:
        params = {}
        if query:
            params["q"] = query
        if filters:
            for key in ("genre", "category"):
                if filters.get(key):
                    params[key] = filters[key]
        suffix = f"?{urlencode(params)}" if params else ""
        return self._build_link("Ver catalogo", f"{self.SEARCH_ROUTE}{suffix}", "CATALOG_SEARCH")

    def build_book_slug(self, book_id: str, title: str) -> str:
        normalized_title = self._slugify(title)
        normalized_id = self._slugify(book_id)
        return f"{normalized_title}-{normalized_id}" if normalized_title else normalized_id

    def build_book_detail_link(self, book_id: str, title: str) -> ChatLink:
        slug = self.build_book_slug(book_id, title)
        return self._build_link("Ver detalle del libro", f"/books/{slug}", "BOOK_DETAIL")

    def build_book_metadata(self, book_id: str, title: str) -> dict:
        slug = self.build_book_slug(book_id, title)
        route = f"/books/{slug}"
        return {
            "selectedBookId": book_id,
            "bookTitle": title,
            "slug": slug,
            "frontendRoute": route,
        }

    def build_cart_link(self) -> ChatLink:
        return self._build_link("Ver carrito", self.CART_ROUTE, "CART")

    def get_initial_suggestions(self) -> list[str]:
        return list(self.INITIAL_SUGGESTIONS)

    def sanitize_internal_path(self, path: str) -> str | None:
        if not path:
            return None
        trimmed = path.strip()
        lowered = trimmed.lower()
        if not trimmed.startswith("/"):
            return None
        if trimmed.startswith("//") or "\\" in trimmed:
            return None
        if lowered.startswith(("/api/", "/api")):
            return None
        path_part = re.split(r"[?#]", trimmed, maxsplit=1)[0]
        if ":" in path_part:
            return None
        if any(value in lowered for value in ("javascript:", "http://", "https://", "data:", "file:", "ftp:", "<script")):
            return None
        return trimmed

    def _build_link(self, label: str, url: str, link_type: str) -> ChatLink:
        safe_url = self.sanitize_internal_path(url)
        if not safe_url:
            raise ValueError("Unsafe frontend route")
        return ChatLink(label=label, url=safe_url, type=link_type)

    def _slugify(self, value: str) -> str:
        without_accents = "".join(
            char
            for char in unicodedata.normalize("NFD", value.lower())
            if unicodedata.category(char) != "Mn"
        )
        slug = re.sub(r"[^a-z0-9]+", "-", without_accents).strip("-")
        return re.sub(r"-+", "-", slug)
