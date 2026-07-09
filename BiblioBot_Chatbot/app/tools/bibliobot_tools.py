from app.clients import MockDotNetClient
from app.services.auth_required_service import AuthRequiredService
from app.services.confirmation_service import ConfirmationService
from app.services.permission_service import PermissionService
from app.tools.tool_context import ToolExecutionContext
from app.tools.tool_schemas import (
    AddOrUpdateCartItemInput,
    CheckStockInput,
    ConfirmSaleInput,
    CreatePurchaseRequestInput,
    CreateSaleFromCartInput,
    CreateTransferRequestInput,
    GetBookDetailInput,
    GetCartInput,
    GetInvoiceInput,
    QueryInventoryInput,
    QuerySalesInput,
    RegisterInventoryEntryInput,
    SearchBooksInput,
)


class BiblioBotToolService:
    def __init__(
        self,
        mock_client: MockDotNetClient | None = None,
        permission_service: PermissionService | None = None,
        confirmation_service: ConfirmationService | None = None,
        auth_required_service: AuthRequiredService | None = None,
    ):
        self.mock_client = mock_client or MockDotNetClient()
        self.permission_service = permission_service or PermissionService()
        self.confirmation_service = confirmation_service or ConfirmationService()
        self.auth_required_service = auth_required_service or AuthRequiredService()

    def search_books(self, input_data: SearchBooksInput, context: ToolExecutionContext) -> dict:
        if not self._has_any_permission(context, ["books.read", "books.search"]):
            return self._permission_denied(["books.read", "books.search"])

        books = self.mock_client.search_books(input_data.query)
        return {
            "status": "MOCK_ONLY",
            "mode": "READ_ONLY",
            "query": input_data.query,
            "resultCount": len(books),
            "books": books,
        }

    def get_book_detail(self, input_data: GetBookDetailInput, context: ToolExecutionContext) -> dict:
        if not self._has_permission(context, "books.read"):
            return self._permission_denied(["books.read"])

        book = self.mock_client.get_book_detail(input_data.book_id)
        if not book:
            return {"status": "NOT_FOUND", "mode": "READ_ONLY", "bookId": input_data.book_id, "book": None}
        return {"status": "MOCK_ONLY", "mode": "READ_ONLY", "book": book}

    def check_stock(self, input_data: CheckStockInput, context: ToolExecutionContext) -> dict:
        if not self._has_any_permission(context, ["books.read", "inventory.read"]):
            return self._permission_denied(["books.read", "inventory.read"])

        stock = self.mock_client.check_stock(input_data.book_id, input_data.branch_id)
        if not stock:
            return {"status": "NOT_FOUND", "mode": "READ_ONLY", "bookId": input_data.book_id, "stock": None}
        return {"status": "MOCK_ONLY", "mode": "READ_ONLY", "stock": stock}

    def get_cart(self, input_data: GetCartInput, context: ToolExecutionContext) -> dict:
        auth_required = self._auth_required_if_guest(context, "cart_read")
        if auth_required:
            return auth_required

        if not self._has_any_permission(context, ["cart.read", "cart.manage"]):
            return self._permission_denied(["cart.read", "cart.manage"])

        cart = self.mock_client.get_cart(input_data.session_id)
        return {"status": "MOCK_ONLY", "mode": "READ_ONLY", "cart": cart}

    def add_or_update_cart_item(
        self,
        input_data: AddOrUpdateCartItemInput,
        context: ToolExecutionContext,
    ) -> dict:
        auth_required = self._auth_required_if_guest(context, "cart_manage")
        if auth_required:
            return auth_required

        if not self._has_any_permission(context, ["cart.manage", "sales.create"]):
            return self._permission_denied(["cart.manage", "sales.create"])

        return self._pending_action(
            context=context,
            intent="cart_item_update",
            summary=f"Preparar carrito con {input_data.quantity} unidad(es) de {input_data.book_id}",
            details=input_data.model_dump(),
        )

    def create_sale_from_cart(self, input_data: CreateSaleFromCartInput, context: ToolExecutionContext) -> dict:
        auth_required = self._auth_required_if_guest(context, "create_sale")
        if auth_required:
            return auth_required

        if not self._has_permission(context, "sales.create"):
            return self._permission_denied(["sales.create"])

        details = input_data.model_dump()
        details["origin_code"] = "CHATBOT"
        draft = self.mock_client.create_sale_draft(
            input_data.session_id,
            branch_id=input_data.branch_id,
        )
        return self._pending_action(
            context=context,
            intent="purchase_intent",
            summary=f"Preparar venta desde carrito de la sesion {input_data.session_id}",
            details={**details, "draft": draft},
        )

    def confirm_sale(self, input_data: ConfirmSaleInput, context: ToolExecutionContext) -> dict:
        auth_required = self._auth_required_if_guest(context, "confirm_sale")
        if auth_required:
            return auth_required

        if not self._has_permission(context, "sales.confirm"):
            return self._permission_denied(["sales.confirm"])

        return self._pending_action(
            context=context,
            intent="sales_confirm",
            summary=f"Preparar confirmacion simulada de venta {input_data.sale_id}",
            details=input_data.model_dump(),
        )

    def get_invoice(self, input_data: GetInvoiceInput, context: ToolExecutionContext) -> dict:
        auth_required = self._auth_required_if_guest(context, "invoice_query")
        if auth_required:
            return auth_required

        if not self._has_any_permission(context, ["invoices.read_own", "invoices.read_all"]):
            return self._permission_denied(["invoices.read_own", "invoices.read_all"])

        if input_data.invoice_id:
            invoice = self.mock_client.get_invoice(input_data.invoice_id)
            if not invoice:
                return {
                    "status": "NOT_FOUND",
                    "mode": "READ_ONLY",
                    "invoiceId": input_data.invoice_id,
                    "invoice": None,
                }
            return {"status": "MOCK_ONLY", "mode": "READ_ONLY", "invoice": invoice}

        return {
            "status": "MOCK_ONLY",
            "mode": "READ_ONLY",
            "saleId": input_data.sale_id,
            "invoice": None,
            "message": "Consulta por sale_id pendiente del cliente real ASP.NET Core.",
        }

    def query_sales(self, input_data: QuerySalesInput, context: ToolExecutionContext) -> dict:
        auth_required = self._auth_required_if_guest(context, "sales_query")
        if auth_required:
            return auth_required

        if input_data.scope == "all" and not self._has_permission(context, "sales.read_all"):
            return self._permission_denied(["sales.read_all"])
        if input_data.scope == "own" and not self._has_any_permission(context, ["sales.read_own", "sales.read_all"]):
            return self._permission_denied(["sales.read_own", "sales.read_all"])

        sales = self.mock_client.query_sales(input_data.scope)
        return {
            "status": "MOCK_ONLY",
            "mode": "READ_ONLY",
            "scope": input_data.scope,
            "resultCount": len(sales),
            "sales": sales,
        }

    def query_inventory(self, input_data: QueryInventoryInput, context: ToolExecutionContext) -> dict:
        auth_required = self._auth_required_if_guest(context, "inventory_query")
        if auth_required:
            return auth_required

        if not self._has_permission(context, "inventory.read"):
            return self._permission_denied(["inventory.read"])

        if input_data.only_low_stock:
            books = self.mock_client.get_low_stock_books()
            return {
                "status": "MOCK_ONLY",
                "mode": "READ_ONLY",
                "onlyLowStock": True,
                "resultCount": len(books),
                "items": books,
            }

        inventory = []
        for book in self.mock_client.search_books():
            stock = self.mock_client.check_stock(book["id"], input_data.branch_id)
            inventory.append(stock)
        return {
            "status": "MOCK_ONLY",
            "mode": "READ_ONLY",
            "branchId": input_data.branch_id,
            "resultCount": len(inventory),
            "items": inventory,
        }

    def register_inventory_entry(
        self,
        input_data: RegisterInventoryEntryInput,
        context: ToolExecutionContext,
    ) -> dict:
        auth_required = self._auth_required_if_guest(context, "inventory_entry")
        if auth_required:
            return auth_required

        if not self._has_permission(context, "inventory.entry"):
            return self._permission_denied(["inventory.entry"])

        simulation = self.mock_client.simulate_inventory_entry(
            input_data.book_id,
            input_data.quantity,
            input_data.branch_id,
        )
        return self._pending_action(
            context=context,
            intent="inventory_entry",
            summary=f"Preparar entrada de {input_data.quantity} unidad(es) para {input_data.book_id}",
            details={**input_data.model_dump(), "simulation": simulation},
        )

    def create_purchase_request(
        self,
        input_data: CreatePurchaseRequestInput,
        context: ToolExecutionContext,
    ) -> dict:
        auth_required = self._auth_required_if_guest(context, "purchase_request")
        if auth_required:
            return auth_required

        if not self._has_permission(context, "requests.purchase.create"):
            return self._permission_denied(["requests.purchase.create"])

        simulation = self.mock_client.simulate_purchase_request(
            input_data.book_id,
            input_data.quantity,
            input_data.notes,
        )
        return self._pending_action(
            context=context,
            intent="purchase_request",
            summary=f"Preparar solicitud de compra de {input_data.quantity} unidad(es) de {input_data.book_id}",
            details={**input_data.model_dump(), "simulation": simulation},
        )

    def create_transfer_request(
        self,
        input_data: CreateTransferRequestInput,
        context: ToolExecutionContext,
    ) -> dict:
        auth_required = self._auth_required_if_guest(context, "transfer_request")
        if auth_required:
            return auth_required

        if not self._has_permission(context, "requests.transfer.create"):
            return self._permission_denied(["requests.transfer.create"])

        simulation = self.mock_client.simulate_transfer_request(
            input_data.book_id,
            input_data.quantity,
            input_data.source_branch_id,
            input_data.destination_branch_id,
        )
        return self._pending_action(
            context=context,
            intent="transfer_request",
            summary=(
                f"Preparar traslado de {input_data.quantity} unidad(es) de {input_data.book_id} "
                f"desde {input_data.source_branch_id} hacia {input_data.destination_branch_id}"
            ),
            details={**input_data.model_dump(), "simulation": simulation},
        )

    def _pending_action(
        self,
        context: ToolExecutionContext,
        intent: str,
        summary: str,
        details: dict,
    ) -> dict:
        action_ref = self.confirmation_service.build_action_ref(context.session_id, intent, summary)
        pending_action = self.confirmation_service.build_pending_action(
            intent=intent,
            action_ref=action_ref,
            summary=summary,
            details=details,
        )
        return {
            "status": "PENDING_CONFIRMATION",
            "mode": "MOCK_ONLY",
            "requiresConfirmation": True,
            "actionRef": action_ref,
            "pendingAction": pending_action,
            "message": "Accion simulada pendiente de confirmacion explicita. No se ejecuto ninguna mutacion real.",
        }

    def _has_permission(self, context: ToolExecutionContext, permission: str) -> bool:
        return self.permission_service.has_permission(context.permissions, permission)

    def _has_any_permission(self, context: ToolExecutionContext, permissions: list[str]) -> bool:
        return self.permission_service.has_any_permission(context.permissions, permissions)

    def _auth_required_if_guest(self, context: ToolExecutionContext, intent: str) -> dict | None:
        if not self.auth_required_service.is_guest_context(context.roles, context.user_id, context.permissions):
            return None
        if not self.auth_required_service.requires_authenticated_user(intent):
            return None

        return {
            "status": "AUTH_REQUIRED",
            "mode": "BLOCKED",
            "requiresAuthentication": True,
            "originalIntent": intent,
            "links": [link.model_dump() for link in self.auth_required_service.build_auth_links()],
            "message": "Para continuar con esa accion necesitas iniciar sesion o crear una cuenta.",
        }

    def _permission_denied(self, required_permissions: list[str]) -> dict:
        return {
            "status": "PERMISSION_DENIED",
            "mode": "BLOCKED",
            "requiredPermissions": required_permissions,
            "message": "No tienes permisos para ejecutar esta tool.",
        }
