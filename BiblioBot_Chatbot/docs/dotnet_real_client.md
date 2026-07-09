# Cliente real ASP.NET Core - Fase 13

## Objetivo

Fase 13 agrega un cliente real controlado para que FastAPI pueda consultar el backend ASP.NET Core cuando se active por configuracion. El cliente mock sigue siendo el modo seguro por defecto.

## Activacion

Variables relevantes:

- `USE_MOCK_DOTNET_CLIENT=true`: usa `MockDotNetClient` y no hace HTTP real.
- `USE_MOCK_DOTNET_CLIENT=false`: usa `DotNetApiClient`.
- `DOTNET_API_BASE_URL`: URL base del backend ASP.NET Core.
- `DOTNET_API_TIMEOUT_SECONDS`: timeout del cliente real. Default: `10`.
- `DOTNET_API_BEARER_TOKEN`: token de servicio opcional. Default: `None`.
- `ALLOW_REAL_BACKEND_MUTATIONS`: habilita mutaciones reales si vale `true`. Default: `false`.

No se deben guardar secretos reales en documentacion ni en el repo.

## Seleccion mock/real

`app.clients.dotnet_client_factory.get_dotnet_client()` centraliza la seleccion:

- con mock activo retorna `MockDotNetClient`;
- con mock desactivado retorna `DotNetApiClient`;
- no hace requests al seleccionar o crear el cliente.

## Endpoints mapeados

Catalogo publico:

- `GET /api/libros`
- `GET /api/libros/search?q=...`
- `GET /api/libros/{id}`

Endpoints protegidos preparados:

- `GET /api/carrito/{sessionId}`
- `POST /api/carrito`
- `POST /api/ventas`
- `POST /api/ventas/{id}/confirmar`
- `GET /api/ventas`
- `GET /api/facturas/{id}`
- `GET /api/facturas/venta/{saleId}`
- `GET /api/inventario`
- `POST /api/inventario/entradas`
- `POST /api/solicitudes/compras`
- `POST /api/solicitudes/traslados`

## Autorizacion

FastAPI recibe `roles` y `permissions` desde ASP.NET Core, pero no recibe el JWT original del usuario. Por eso el cliente real no inventa autorizacion, no convierte roles en `Authorization` y no omite la autorizacion del backend.

Si `DOTNET_API_BEARER_TOKEN` existe, se envia como token de servicio. Si no existe, no se envia header `Authorization`.

Ante `401` se usa error seguro `backend_unauthorized`; ante `403`, `permission_denied`.

## Mutaciones reales

Las mutaciones reales quedan bloqueadas por defecto con `ALLOW_REAL_BACKEND_MUTATIONS=false`. En ese modo, los metodos de escritura levantan `DotNetApiMutationDisabledError` antes de hacer HTTP.

Las tools conversacionales siguen preparando acciones pendientes de confirmacion y no ejecutan ventas, carrito, inventario ni solicitudes reales de forma accidental.

## Manejo de errores

Mapeo seguro:

- `400`: `bad_request`
- `401`: `backend_unauthorized`
- `403`: `permission_denied`
- `404`: `not_found`
- `409`: `conflict`
- timeout: `backend_timeout`
- conexion fallida o `500+`: `backend_unavailable`
- JSON invalido: `backend_invalid_response`

No se exponen stack traces, tokens, HTML crudo, ni URLs internas con datos sensibles.

## Pruebas

Las pruebas usan `httpx.MockTransport`. No requieren backend real encendido y no hacen E2E real. La Fase 14 queda reservada para pruebas E2E contra ASP.NET Core real.

## Alcance

Esta fase solo modifica `BiblioBot_Chatbot/`. No toca frontend, backend, Docker, `.env`, PostgreSQL, pgvector ni OpenAI.
