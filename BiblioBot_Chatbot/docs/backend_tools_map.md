# BiblioBot Chatbot - Backend Tools Map

Fase 9 define tools controladas para uso futuro con LangGraph. En esta fase todas operan sobre `MockDotNetClient`; no hacen HTTP real ni ejecutan mutaciones reales.

| Tool | Descripcion | Endpoint real futuro ASP.NET Core | Permiso requerido | Requiere confirmacion | Estado actual |
|---|---|---|---|---|---|
| `search_books` | Busca libros por texto o devuelve catalogo simulado | `GET /api/libros/search?q=...` | `books.read` o `books.search` | No | `MOCK_ONLY` / `READ_ONLY` |
| `get_book_detail` | Obtiene detalle de un libro | `GET /api/libros/{id}` | `books.read` | No | `MOCK_ONLY` / `READ_ONLY` |
| `check_stock` | Consulta stock total o por sede | `GET /api/libros/{id}` o `GET /api/inventario` | `books.read` o `inventory.read` | No | `MOCK_ONLY` / `READ_ONLY` |
| `get_cart` | Consulta carrito por sesion | `GET /api/carrito/{sessionId}` | `cart.read` o `cart.manage` | No | `MOCK_ONLY` / `READ_ONLY` |
| `add_or_update_cart_item` | Prepara alta o cambio de item de carrito | `POST /api/carrito` | `cart.manage` | Si | `PENDING_CONFIRMATION` / `MOCK_ONLY` |
| `create_sale_from_cart` | Prepara venta desde carrito | `POST /api/ventas` | `sales.create` | Si | `PENDING_CONFIRMATION` / `MOCK_ONLY` |
| `confirm_sale` | Prepara confirmacion de venta | `POST /api/ventas/{id}/confirmar` | `sales.confirm` | Si | `PENDING_CONFIRMATION` / `MOCK_ONLY` |
| `get_invoice` | Consulta factura por invoice_id o prepara consulta por sale_id | `GET /api/facturas/{id}` o `GET /api/facturas/venta/{saleId}` | `invoices.read_own` o `invoices.read_all` | No | `MOCK_ONLY` / `READ_ONLY` |
| `query_sales` | Consulta ventas simuladas en alcance `own` o `all` | `GET /api/ventas` | `sales.read_own` o `sales.read_all` | No | `MOCK_ONLY` / `READ_ONLY` |
| `query_inventory` | Consulta inventario o stock bajo simulado | `GET /api/inventario` | `inventory.read` | No | `MOCK_ONLY` / `READ_ONLY` |
| `register_inventory_entry` | Prepara entrada de inventario | `POST /api/inventario/entradas` | `inventory.entry` | Si | `PENDING_CONFIRMATION` / `MOCK_ONLY` |
| `create_purchase_request` | Prepara solicitud interna de compra | `POST /api/solicitudes/compras` | `requests.purchase.create` | Si | `PENDING_CONFIRMATION` / `MOCK_ONLY` |
| `create_transfer_request` | Prepara solicitud interna de traslado | `POST /api/solicitudes/traslados` | `requests.transfer.create` | Si | `PENDING_CONFIRMATION` / `MOCK_ONLY` |

Reglas de seguridad:
- Las tools reciben `ToolExecutionContext` con permisos ya validados por ASP.NET Core.
- No se asumen permisos por rol.
- Las acciones sensibles devuelven `PENDING_CONFIRMATION` y `pendingAction`.
- Ninguna tool confirma ventas, registra inventario o crea solicitudes reales en Fase 9.
