import hashlib
from typing import Any


class ResponseComposerService:
    """Builds controlled chatbot responses from trusted state and metadata."""

    def compose(self, state: dict[str, Any]) -> str:
        base_response = self._clean_text(
            state.get("response")
            or "No pude preparar una respuesta segura. Intenta reformular tu solicitud."
        )
        intent = str(state.get("intent") or "unknown")
        next_step = str(state.get("next_step") or "")
        chat_state = str(state.get("state") or "")
        metadata = self._mapping(state.get("metadata"))

        if intent == "out_of_domain" or next_step == "OUT_OF_DOMAIN":
            return self._compose_out_of_domain(state)
        if intent == "auth_required" or next_step == "AUTH_REQUIRED":
            return self._compose_auth_required(state)
        if next_step == "PERMISSION_DENIED":
            return self._compose_permission_denied(state, base_response)
        if intent == "greeting":
            return self._compose_greeting(state)
        if intent == "identity_help":
            return self._compose_identity_help(state)
        if intent == "admin_navigation":
            return self._compose_admin_navigation(state, base_response)
        if intent == "admin_inventory_adjustment":
            return self._compose_admin_inventory_adjustment(state, base_response)
        if metadata.get("cartUpdated") is True or next_step == "CART_UPDATED":
            return self._compose_cart_updated(state, base_response)
        if next_step == "CONFIRMATION_RECEIVED_MUTATION_BLOCKED":
            return self._compose_confirmed_safe_mode(state, base_response)
        if chat_state == "WAITING_CONFIRMATION" and state.get("requires_confirmation"):
            return self._compose_pending_confirmation(state, base_response)
        if next_step == "BOOK_RECOMMENDATION_READY":
            return base_response
        if intent in {"catalog_search", "refine_catalog_filter"}:
            return self._compose_catalog_search(state, base_response)
        if intent == "book_detail":
            return self._compose_book_detail(state, base_response)
        if intent in {"stock_check", "stock_context_query", "stock_explicit_query"}:
            return self._compose_stock_check(state, base_response)
        if intent == "list_categories":
            return self._compose_list_categories(state, base_response)
        if intent == "general_help":
            return self._compose_general_help(state)

        return base_response

    def select_variant(self, variants: list[str], session_id: str, intent: str, message: str) -> str:
        if not variants:
            return ""
        seed = f"{session_id}:{intent}:{message}".encode("utf-8")
        digest = hashlib.sha256(seed).hexdigest()
        return variants[int(digest[:8], 16) % len(variants)]

    def _compose_out_of_domain(self, state: dict[str, Any]) -> str:
        variants = [
            "Ese tema no hace parte de BiblioBot. Puedo ayudarte con libros, catalogo, categorias, disponibilidad, compras, facturas o inventario.",
            "Desde BiblioBot solo puedo ayudarte con la biblioteca, el catalogo, stock, carrito, ventas, facturas o inventario.",
            "No tengo permitido responder ese tema aqui. Si quieres, buscamos un libro, una categoria o revisamos disponibilidad.",
        ]
        return self._variant(state, "out_of_domain", variants)

    def _compose_greeting(self, state: dict[str, Any]) -> str:
        variants = [
            "Hola, soy BiblioBot. Estoy aqui para ayudarte a encontrar libros, revisar disponibilidad o moverte por la pagina.",
            "Hola. Soy tu copiloto dentro de BiblioBot: puedo ayudarte con catalogo, libros, stock y compras seguras.",
            "Hola, que bueno verte por aqui. Dime si quieres buscar un libro, revisar categorias o consultar disponibilidad.",
        ]
        return self._variant(state, "greeting", variants)

    def _compose_identity_help(self, state: dict[str, Any]) -> str:
        variants = [
            "Soy BiblioBot, el copiloto de esta biblioteca virtual. Te ayudo con libros, categorias, disponibilidad, compras y tareas administrativas si tienes permisos.",
            "Soy BiblioBot. Estoy dentro de la pagina para ayudarte a buscar libros, revisar stock, guiar compras y orientar modulos del sistema segun tus permisos.",
            "Soy tu asistente de BiblioBot: puedo ayudarte con catalogo, detalles de libros, stock, carrito, facturas e inventario autorizado.",
        ]
        return self._variant(state, "identity_help", variants)

    def _compose_general_help(self, state: dict[str, Any]) -> str:
        capabilities = self._describe_capabilities(state)
        variants = [
            f"Soy BiblioBot. Puedo ayudarte con {capabilities}. Podemos empezar por buscar un libro, revisar categorias o consultar disponibilidad.",
            f"Estoy para ayudarte en BiblioBot con {capabilities}. Dime un titulo, autor, categoria o la accion que necesitas revisar.",
            f"Puedo orientarte con {capabilities}. Si tienes un libro en mente, dime el titulo; si no, puedo mostrarte categorias o recomendaciones.",
        ]
        return self._variant(state, "general_help", variants)

    def _compose_permission_denied(self, state: dict[str, Any], base_response: str) -> str:
        variants = [
            "No tienes el permiso necesario para esa accion. Puedo ayudarte con otra consulta disponible para tu cuenta.",
            "Esa accion requiere el permiso necesario antes de continuar. Puedo orientarte con opciones disponibles para tu cuenta.",
            "No puedo abrir esa accion sin el permiso necesario. Mantengo la seguridad y puedo ayudarte con otra cosa.",
        ]
        return self._variant(state, "permission_denied", variants) if base_response else self._variant(state, "permission_denied", variants)

    def _compose_catalog_search(self, state: dict[str, Any], base_response: str) -> str:
        if self._backend_error(state):
            return base_response

        metadata = self._mapping(state.get("metadata"))
        books = self._books_from_metadata_or_result(state)
        if not books:
            variants = [
                "No encontre coincidencias por ahora. Prueba con otro titulo, autor o categoria y vuelvo a buscar.",
                "Revise el catalogo y no tengo resultados claros para esa busqueda. Puedes intentar con una categoria o un titulo mas especifico.",
                "Por ahora no aparecen libros que coincidan. Si me das otro dato, como autor o genero, filtro de nuevo.",
            ]
            return self._variant(state, str(state.get("intent") or "catalog_search"), variants)

        titles = self._join_titles(books)
        result_count = metadata.get("resultCount") or metadata.get("filteredCount") or len(books)
        variants = [
            f"Encontre {result_count} resultado(s) en el catalogo. Algunas opciones son: {titles}. Te dejo la vista filtrada para revisarlas.",
            f"Hay opciones relacionadas en el catalogo: {titles}. Puedes abrir el resultado y comparar disponibilidad o precio.",
            f"Claro, filtre el catalogo y encontre: {titles}. Si quieres, despues puedo ayudarte a revisar un libro puntual.",
        ]
        return self._variant(state, str(state.get("intent") or "catalog_search"), variants)

    def _compose_book_detail(self, state: dict[str, Any], base_response: str) -> str:
        if self._backend_error(state):
            return base_response

        metadata = self._mapping(state.get("metadata"))
        book = self._book_from_state(state)
        if not book:
            return base_response

        title = self._book_title(book)
        author = self._book_author(book)
        genre = self._book_genre(book)
        price = self._format_price(book.get("price"))
        availability = self._availability_text(book, metadata.get("stock"))
        description = self._clean_text(book.get("description"))

        if metadata.get("summaryRequested"):
            if description:
                variants = [
                    f"{title}, de {author}, tiene esta sinopsis registrada: {description} Datos del catalogo: categoria {genre}, precio {price} y disponibilidad {availability}.",
                    f"Segun el catalogo, {title} es de {author}. Sinopsis registrada: {description} Tambien figura en {genre}, con precio {price} y disponibilidad {availability}.",
                    f"Te resumo solo con datos registrados: {title}, autor {author}. {description} Categoria: {genre}. Precio: {price}. Disponibilidad: {availability}.",
                ]
            else:
                variants = [
                    f"No tengo una sinopsis extensa registrada para {title}. Con los datos disponibles: autor {author}, categoria {genre}, precio {price} y disponibilidad {availability}.",
                    f"Para {title} no aparece una descripcion larga en el catalogo. Lo que si tengo es: autor {author}, categoria {genre}, precio {price} y disponibilidad {availability}.",
                    f"Puedo darte los datos reales registrados de {title}: autor {author}, categoria {genre}, precio {price} y disponibilidad {availability}. No agrego trama si no esta en el catalogo.",
                ]
            return self._variant(state, "book_detail", variants)

        variants = [
            f"Encontre {title}. Autor: {author}. Categoria: {genre}. Precio: {price}. Disponibilidad: {availability}. Te dejo el detalle para revisarlo.",
            f"Tengo el detalle de {title}: autor {author}, categoria {genre}, precio {price} y disponibilidad {availability}.",
            f"Este es el libro encontrado: {title}, de {author}. Figura en {genre}, con precio {price} y disponibilidad {availability}.",
        ]
        return self._variant(state, "book_detail", variants)

    def _compose_stock_check(self, state: dict[str, Any], base_response: str) -> str:
        if self._backend_error(state):
            return base_response

        metadata = self._mapping(state.get("metadata"))
        stock = self._mapping(metadata.get("stock"))
        if not stock:
            tool_result = self._mapping(state.get("tool_result"))
            stock = self._mapping(tool_result.get("stock"))
        if not stock:
            return base_response

        title = self._clean_text(stock.get("title") or stock.get("bookTitle") or "ese libro")
        total = stock.get("totalStock", stock.get("stock"))
        total_text = f"{total} unidad(es)" if total is not None else "disponibilidad no especificada"
        branches = self._branch_stock_text(stock)
        variants = [
            f"Encontre disponibilidad para {title}: {total_text} en total{branches}.",
            f"{title} tiene {total_text} registradas{branches}.",
            f"Disponibilidad de {title}: {total_text} en el inventario consultado{branches}.",
        ]
        return self._variant(state, str(state.get("intent") or "stock_check"), variants)

    def _compose_list_categories(self, state: dict[str, Any], base_response: str) -> str:
        if self._backend_error(state):
            return base_response

        metadata = self._mapping(state.get("metadata"))
        categories = self._text_list(metadata.get("categories"))
        if not categories:
            variants = [
                "Todavia no encontre categorias disponibles en el catalogo.",
                "No tengo categorias registradas para mostrar en este momento.",
                "Por ahora el catalogo no devuelve categorias disponibles.",
            ]
            return self._variant(state, "list_categories", variants)

        category_text = ", ".join(categories[:12])
        variants = [
            f"Estas son categorias disponibles: {category_text}. Puedes pedirme libros de cualquiera de ellas.",
            f"En el catalogo aparecen estas categorias: {category_text}. Dime una y filtro libros por ti.",
            f"Tengo estas categorias para explorar: {category_text}. Si quieres, buscamos recomendaciones en una categoria concreta.",
        ]
        return self._variant(state, "list_categories", variants)

    def _compose_auth_required(self, state: dict[str, Any]) -> str:
        metadata = self._mapping(state.get("metadata"))
        original_intent = str(metadata.get("originalIntent") or state.get("intent") or "")
        if original_intent in {"purchase_intent", "checkout_cart", "confirm_sale", "cart_manage", "cart_read", "create_sale"}:
            variants = [
                "Puedo ayudarte a explorar el catalogo, pero para comprar o usar el carrito necesitas iniciar sesion o crear una cuenta.",
                "Para continuar con compras, carrito o confirmaciones necesitas iniciar sesion o crear una cuenta. Mientras tanto puedo ayudarte a buscar libros.",
                "Esa accion requiere cuenta activa: necesitas iniciar sesion o crear una cuenta antes de comprar o usar el carrito.",
            ]
        else:
            variants = [
                "Para continuar con esa accion necesitas iniciar sesion o crear una cuenta. Mientras tanto puedo ayudarte a explorar el catalogo.",
                "Necesitas iniciar sesion o crear una cuenta para esa accion. Si quieres, puedo orientarte con busquedas del catalogo.",
                "Esa operacion requiere autenticacion: necesitas iniciar sesion o crear una cuenta antes de continuar.",
            ]
        return self._variant(state, "auth_required", variants)

    def _compose_admin_navigation(self, state: dict[str, Any], base_response: str) -> str:
        metadata = self._mapping(state.get("metadata"))
        target = self._clean_text(metadata.get("adminTarget") or "admin")
        if target == "users":
            variants = [
                "Claro, te llevo al modulo de usuarios para continuar desde el panel.",
                "Vamos al panel de usuarios. Alli puedes revisar o crear cuentas segun tus permisos.",
                "Te abro usuarios para que sigas la gestion desde el modulo administrativo.",
            ]
        elif target == "inventory":
            variants = [
                "Claro, te llevo al modulo de inventario.",
                "Abramos inventario para revisar stock y movimientos desde el panel.",
                "Te llevo a inventario para continuar con datos del sistema.",
            ]
        else:
            variants = [
                base_response,
                "Te llevo al modulo disponible para revisar esa informacion administrativa.",
                "Abramos el panel correspondiente para continuar de forma segura.",
            ]
        return self._variant(state, "admin_navigation", [variant for variant in variants if variant])

    def _compose_admin_inventory_adjustment(self, state: dict[str, Any], base_response: str) -> str:
        metadata = self._mapping(state.get("metadata"))
        title = self._clean_text(metadata.get("bookTitle") or "el libro seleccionado")
        adjustment_type = metadata.get("adjustmentType")
        quantity = metadata.get("quantity")
        expected = metadata.get("expectedStockAfter")
        direction = "salida" if adjustment_type == "OUT" else "entrada"
        quantity_text = f" de {quantity} unidad(es)" if quantity else ""
        expected_text = f" para dejar el stock en {expected}" if expected is not None else ""
        variants = [
            f"Puedo ayudarte con esa {direction}{quantity_text} de {title}{expected_text}. Te llevo a inventario para revisarlo y confirmarlo desde el panel.",
            f"No cambio stock directamente desde el chat. Te abro inventario con {title} como referencia para que confirmes el ajuste{expected_text}.",
            f"Entendi el ajuste de inventario para {title}. Lo seguro es revisarlo en el modulo de inventario antes de guardar cambios.",
        ]
        return self._variant(state, "admin_inventory_adjustment", variants) if title else base_response

    def _compose_pending_confirmation(self, state: dict[str, Any], base_response: str) -> str:
        intent = str(state.get("intent") or "")
        pending_action = self._pending_action(state)
        details = self._mapping(pending_action.get("details"))
        title = self._clean_text(pending_action.get("bookTitle") or details.get("bookTitle") or "el libro seleccionado")
        quantity = pending_action.get("quantity") or details.get("quantity")

        if intent == "purchase_intent":
            quantity_text = f"{quantity} unidad(es) de " if quantity else ""
            variants = [
                f"Perfecto. Tengo preparada la accion para agregar {quantity_text}{title} en modo seguro. Antes de continuar, necesito que confirmes si deseas realizarla.",
                f"Ya deje lista la solicitud sobre {quantity_text}{title}. Para evitar cambios accidentales, necesito que confirmes antes de continuar.",
                f"Puedo avanzar con {quantity_text}{title}, pero primero necesito que confirmes la accion pendiente.",
            ]
            return self._variant(state, "purchase_intent", variants)

        if intent == "checkout_cart":
            variants = [
                "Tengo listo el checkout del carrito en modo seguro. Antes de continuar, necesito que confirmes si deseas realizarlo.",
                "Puedo preparar la venta desde el carrito, pero necesito que confirmes antes de seguir.",
                "El carrito esta listo para pasar a venta pendiente. Para evitar cambios accidentales, necesito que confirmes la accion.",
            ]
            return self._variant(state, "checkout_cart", variants)

        if intent == "confirm_sale":
            sale_id = self._clean_text(pending_action.get("saleId") or details.get("saleId") or "")
            sale_text = f" {sale_id}" if sale_id else ""
            variants = [
                f"Tengo preparada la confirmacion de venta{sale_text} en modo seguro. Necesito que confirmes antes de continuar.",
                f"Puedo validar la confirmacion de venta{sale_text}, pero primero necesito que confirmes la accion pendiente.",
                f"Antes de tocar la venta{sale_text}, necesito que confirmes explicitamente esta accion.",
            ]
            return self._variant(state, "confirm_sale", variants)

        return base_response

    def _compose_cart_updated(self, state: dict[str, Any], base_response: str) -> str:
        metadata = self._mapping(state.get("metadata"))
        title = self._clean_text(metadata.get("bookTitle") or metadata.get("title") or "el libro")
        quantity = metadata.get("quantity")
        quantity_text = f"{quantity} unidad(es) de " if quantity else ""
        variants = [
            f"Actualice el carrito con {quantity_text}{title}. Puedes seguir explorando o pasar al checkout cuando quieras.",
            f"El carrito quedo actualizado con {quantity_text}{title}. Si necesitas cambiar cantidades, dime el ajuste.",
            f"Listo: {quantity_text}{title} quedo en el carrito. Cuando quieras, puedo ayudarte a finalizar la compra.",
        ]
        return self._variant(state, "cart_updated", variants) if title else base_response

    def _compose_confirmed_safe_mode(self, state: dict[str, Any], base_response: str) -> str:
        variants = [
            "Confirmacion recibida. La accion quedo validada en modo seguro; no se ejecuto ninguna compra, venta ni inventario real.",
            "Recibi tu confirmacion. Mantengo la accion en modo seguro y no realizo mutaciones reales sobre compras, ventas o inventario.",
            "Confirmado en modo seguro. La validacion quedo registrada en la respuesta, sin ejecutar cambios reales.",
        ]
        return self._variant(state, "confirm_sale", variants) or base_response

    def _variant(self, state: dict[str, Any], intent: str, variants: list[str]) -> str:
        return self.select_variant(
            variants,
            str(state.get("session_id") or ""),
            intent,
            str(state.get("message") or ""),
        )

    def _books_from_metadata_or_result(self, state: dict[str, Any]) -> list[dict[str, Any]]:
        metadata = self._mapping(state.get("metadata"))
        books = metadata.get("books") or metadata.get("resultBooks")
        if not isinstance(books, list):
            tool_result = self._mapping(state.get("tool_result"))
            books = tool_result.get("books")
        return [book for book in books if isinstance(book, dict)] if isinstance(books, list) else []

    def _book_from_state(self, state: dict[str, Any]) -> dict[str, Any]:
        metadata = self._mapping(state.get("metadata"))
        metadata_book = self._mapping(metadata.get("book"))
        tool_book = self._mapping(self._mapping(state.get("tool_result")).get("book"))
        return {**metadata_book, **tool_book}

    def _pending_action(self, state: dict[str, Any]) -> dict[str, Any]:
        pending_action = self._mapping(state.get("pending_action"))
        if pending_action:
            return pending_action
        metadata = self._mapping(state.get("metadata"))
        return self._mapping(metadata.get("pendingAction"))

    def _describe_capabilities(self, state: dict[str, Any]) -> str:
        permissions = self._text_list(state.get("permissions"))
        metadata = self._mapping(state.get("metadata"))
        if not permissions:
            permissions = self._text_list(metadata.get("permissions"))

        capabilities = []
        if self._has_any(permissions, {"books.read", "books.search"}):
            capabilities.append("buscar libros, mostrar categorias y revisar detalles")
        if self._has_any(permissions, {"inventory.read"}):
            capabilities.append("consultar disponibilidad")
        if self._has_any(permissions, {"cart.manage", "sales.create"}):
            capabilities.append("preparar compras con confirmacion")
        if self._has_any(permissions, {"invoices.read_own", "invoices.read_all"}):
            capabilities.append("consultar facturas")
        if self._has_any(permissions, {"sales.read_own", "sales.read_all", "sales.confirm"}):
            capabilities.append("revisar o confirmar ventas segun tus permisos")
        if self._has_any(permissions, {"inventory.entry"}):
            capabilities.append("preparar entradas de inventario")
        if self._has_any(permissions, {"requests.transfer.create", "requests.purchase.create"}):
            capabilities.append("preparar solicitudes internas")

        return ", ".join(capabilities) if capabilities else "orientarte sobre el catalogo y las funciones disponibles"

    def _availability_text(self, book: dict[str, Any], stock: Any) -> str:
        stock_map = self._mapping(stock)
        if stock_map:
            total = stock_map.get("totalStock", stock_map.get("stock"))
            if total is not None:
                return f"{total} unidad(es)"
        if isinstance(book.get("available"), bool):
            return "disponible" if book["available"] else "sin disponibilidad registrada"
        return "no especificada"

    def _branch_stock_text(self, stock: dict[str, Any]) -> str:
        by_branch = stock.get("stockByBranch") or stock.get("branches")
        if not isinstance(by_branch, dict):
            return ""
        parts = [f"{branch}: {quantity}" for branch, quantity in list(by_branch.items())[:4]]
        return f" ({'; '.join(parts)})" if parts else ""

    def _book_title(self, book: dict[str, Any]) -> str:
        return self._clean_text(book.get("title") or "Libro sin titulo")

    def _book_author(self, book: dict[str, Any]) -> str:
        author = book.get("author")
        if author:
            return self._clean_text(author)
        authors = self._text_list(book.get("authors"))
        return ", ".join(authors) if authors else "autor no especificado"

    def _book_genre(self, book: dict[str, Any]) -> str:
        genre = book.get("genre")
        if genre:
            return self._clean_text(genre)
        categories = self._text_list(book.get("categories"))
        return ", ".join(categories) if categories else "categoria no especificada"

    def _join_titles(self, books: list[dict[str, Any]], limit: int = 3) -> str:
        titles = [self._book_title(book) for book in books[:limit] if self._book_title(book)]
        return "; ".join(titles) if titles else "resultados sin titulo"

    def _format_price(self, value: Any) -> str:
        if isinstance(value, bool) or not isinstance(value, (int, float)):
            return "no especificado"
        if isinstance(value, float) and not value.is_integer():
            return f"${value:,.2f}".replace(",", "_").replace(".", ",").replace("_", ".")
        return f"${int(value):,}".replace(",", ".")

    def _backend_error(self, state: dict[str, Any]) -> bool:
        tool_result = self._mapping(state.get("tool_result"))
        return tool_result.get("status") in {"BACKEND_ERROR", "AUTH_REQUIRED", "PERMISSION_DENIED"} and bool(tool_result.get("errorCode"))

    def _has_any(self, values: list[str], expected: set[str]) -> bool:
        return any(value in expected for value in values)

    def _mapping(self, value: Any) -> dict[str, Any]:
        return value if isinstance(value, dict) else {}

    def _text_list(self, value: Any) -> list[str]:
        if not isinstance(value, list):
            return []
        return [self._clean_text(item) for item in value if self._clean_text(item)]

    def _clean_text(self, value: Any) -> str:
        if value is None:
            return ""
        return " ".join(str(value).split())
