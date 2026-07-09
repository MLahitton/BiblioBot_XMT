from collections.abc import Callable
from typing import Any

from langchain_core.tools import StructuredTool

from app.tools.bibliobot_tools import BiblioBotToolService
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


def get_langchain_tools(tool_service: BiblioBotToolService | None = None) -> list[StructuredTool]:
    service = tool_service or BiblioBotToolService()
    specs = [
        (
            "search_books",
            "Busca libros en el catalogo simulado. Requiere books.read o books.search.",
            SearchBooksInput,
            service.search_books,
        ),
        (
            "get_book_detail",
            "Obtiene detalle de un libro simulado. Requiere books.read.",
            GetBookDetailInput,
            service.get_book_detail,
        ),
        (
            "check_stock",
            "Consulta stock simulado. Requiere books.read o inventory.read.",
            CheckStockInput,
            service.check_stock,
        ),
        (
            "get_cart",
            "Consulta carrito simulado. Requiere cart.read o cart.manage.",
            GetCartInput,
            service.get_cart,
        ),
        (
            "add_or_update_cart_item",
            "Prepara cambio de carrito sin mutacion real. Requiere cart.manage y confirmacion.",
            AddOrUpdateCartItemInput,
            service.add_or_update_cart_item,
        ),
        (
            "create_sale_from_cart",
            "Prepara venta simulada desde carrito. Requiere sales.create y confirmacion.",
            CreateSaleFromCartInput,
            service.create_sale_from_cart,
        ),
        (
            "confirm_sale",
            "Prepara confirmacion simulada de venta. Requiere sales.confirm y confirmacion.",
            ConfirmSaleInput,
            service.confirm_sale,
        ),
        (
            "get_invoice",
            "Consulta factura simulada. Requiere invoices.read_own o invoices.read_all.",
            GetInvoiceInput,
            service.get_invoice,
        ),
        (
            "query_sales",
            "Consulta ventas simuladas por alcance own/all. Requiere permisos de ventas.",
            QuerySalesInput,
            service.query_sales,
        ),
        (
            "query_inventory",
            "Consulta inventario simulado. Requiere inventory.read.",
            QueryInventoryInput,
            service.query_inventory,
        ),
        (
            "register_inventory_entry",
            "Prepara entrada de inventario simulada. Requiere inventory.entry y confirmacion.",
            RegisterInventoryEntryInput,
            service.register_inventory_entry,
        ),
        (
            "create_purchase_request",
            "Prepara solicitud interna de compra simulada. Requiere requests.purchase.create y confirmacion.",
            CreatePurchaseRequestInput,
            service.create_purchase_request,
        ),
        (
            "create_transfer_request",
            "Prepara solicitud interna de traslado simulada. Requiere requests.transfer.create y confirmacion.",
            CreateTransferRequestInput,
            service.create_transfer_request,
        ),
    ]
    return [
        StructuredTool.from_function(
            func=_build_runner(schema, method),
            name=name,
            description=description,
        )
        for name, description, schema, method in specs
    ]


def _build_runner(input_schema, method: Callable[[Any, ToolExecutionContext], dict]):
    def run(input_data: dict, context: dict) -> dict:
        parsed_input = input_schema.model_validate(input_data)
        parsed_context = ToolExecutionContext.model_validate(context)
        return method(parsed_input, parsed_context)

    return run
