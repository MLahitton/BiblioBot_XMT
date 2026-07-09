# Fase 10 - Flujo Conversacional con LangGraph

## Objetivo

El grafo organiza el procesamiento conversacional del chatbot sin cambiar el contrato externo de `POST /chat/process`.
FastAPI sigue devolviendo `response`, `state`, `links`, `uiAction` y `context`.

## Nodos

- `normalize_input`: copia el request validado, normaliza el mensaje y prepara metadata interna.
- `base_validation`: valida `sessionId`, permiso `chat.message` y presencia de roles.
- `confirmation_control`: detecta confirmaciones o cancelaciones explicitas sin ejecutar acciones.
- `intent_detection`: detecta intencion deterministica y usa Gemini solo como apoyo opcional.
- `permission_check`: usa `PermissionService` como fuente de autorizacion.
- `tool_dispatch`: usa `BiblioBotToolService` y datos mock para lecturas o acciones pendientes.
- `response_builder`: arma texto conversacional y permite mejora segura con `LlmAssistantService`.
- `final_safety`: valida estado, `uiAction`, links, metadata y reglas de acciones sensibles.

## Orden del flujo

`START -> normalize_input -> base_validation -> confirmation_control -> intent_detection -> permission_check -> tool_dispatch -> response_builder -> final_safety -> END`

Las validaciones base, confirmaciones y permisos pueden ir directo a `final_safety` si producen una respuesta terminal.

## Seguridad

LangGraph no autoriza permisos, no confirma acciones y no ejecuta mutaciones reales.
Las autorizaciones dependen de `PermissionService`.
Las confirmaciones dependen de `ConfirmationService`.
Las tools sensibles solo devuelven `PENDING_CONFIRMATION`, `requiresConfirmation`, `actionRef` y `pendingAction`.

## Navegacion visual

FastAPI no navega ni cierra el chat.
Para busquedas de catalogo puede devolver `NAVIGATE_TO_CATALOG` con `context.metadata.query` y `context.metadata.filters`.
Desde Fase 12, las busquedas incluyen metadata visual hacia `/search`.
Para detalle de libro puede devolver `NAVIGATE_TO_PRODUCT`, `selectedBookId` y un link interno controlado `/books/{slug}`.
React interpreta estas senales y decide si navega internamente manteniendo abierto el chat.

## Servicios usados

- `PermissionService`: valida permisos explicitos por intencion, sin inferir por rol.
- `ConfirmationService`: detecta confirmaciones/cancelaciones y crea referencias mock.
- `BiblioBotToolService`: centraliza tools controladas y datos mock.
- `LlmAssistantService/Gemini`: solo sugiere intencion permitida o mejora texto visible.
- `MockDotNetClient`: simula lecturas y preparaciones mock, sin HTTP real.

## Limites de esta fase

No hay HTTP real hacia ASP.NET Core.
No hay conexion a base de datos.
No hay OpenAI.
No hay persistencia conversacional real.
No hay mutaciones reales de ventas, inventario o solicitudes.
