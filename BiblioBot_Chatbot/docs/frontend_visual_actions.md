# Frontend visual actions

## Objetivo

Este documento describe las senales visuales que el chatbot FastAPI devuelve al backend ASP.NET Core para que React/Next pueda decidir navegacion interna sin cerrar el chat.

FastAPI no ejecuta navegacion real. Solo devuelve `uiAction`, `links` y metadata segura dentro de `context`.

## Contrato actual

La respuesta conserva la forma externa:

```json
{
  "response": "Texto visible para el usuario",
  "state": "INTENT_DETECTED",
  "links": [],
  "uiAction": "NONE",
  "context": {}
}
```

No se agregan campos nuevos en la raiz. Las sugerencias viven en `context.metadata.suggestions`.

## Acciones permitidas

- `NAVIGATE_TO_CATALOG`: React puede abrir o actualizar la vista de busqueda.
- `NAVIGATE_TO_PRODUCT`: React puede abrir el detalle del libro.
- `OPEN_CART`: React puede abrir el carrito.
- `SHOW_INVOICE`: React decide como mostrar la factura; el chatbot no inventa ruta.
- `APPLY_FILTERS`: reservado para filtros visuales.
- `NONE`: no hay accion visual.

## Rutas frontend

- Busqueda/catalogo: `/search`
- Detalle de libro: `/books/{slug}`
- Carrito: `/cart`
- Login: `/auth/login`
- Registro: `/auth/register`

No se deben usar rutas backend `/api/*` como links visuales.

## Links

Los links son rutas internas seguras. Se bloquean rutas vacias, externas, `javascript:`, `data:`, `file:`, rutas locales con `:`, rutas con `//`, backslashes y rutas `/api/*`.

## Metadata

La metadata visual se entrega dentro de `context.metadata`.

### Catalog search

```json
{
  "uiAction": "NAVIGATE_TO_CATALOG",
  "links": [
    {
      "label": "Ver catalogo",
      "url": "/search?q=fantasia&genre=fantasia",
      "type": "CATALOG_SEARCH"
    }
  ],
  "context": {
    "intent": "catalog_search",
    "metadata": {
      "frontendRoute": "/search",
      "query": "fantasia",
      "filters": {
        "genre": "fantasia"
      },
      "genre": "fantasia"
    }
  }
}
```

### Book detail

```json
{
  "uiAction": "NAVIGATE_TO_PRODUCT",
  "links": [
    {
      "label": "Ver detalle del libro",
      "url": "/books/python-practico-book-003",
      "type": "BOOK_DETAIL"
    }
  ],
  "context": {
    "selectedBookId": "book-003",
    "metadata": {
      "selectedBookId": "book-003",
      "bookTitle": "Python Practico",
      "slug": "python-practico-book-003",
      "frontendRoute": "/books/python-practico-book-003"
    }
  }
}
```

### Auth required

```json
{
  "uiAction": "NONE",
  "links": [
    {
      "label": "Iniciar sesion",
      "url": "/auth/login",
      "type": "AUTH_LOGIN"
    },
    {
      "label": "Crear cuenta",
      "url": "/auth/register",
      "type": "AUTH_REGISTER"
    }
  ],
  "context": {
    "intent": "auth_required",
    "nextAction": "AUTH_REQUIRED"
  }
}
```

### Open cart

`OPEN_CART` se asocia con `/cart`. La Fase 12 no fuerza apertura de carrito para acciones sensibles que aun estan en `PENDING_CONFIRMATION`.

```json
{
  "uiAction": "OPEN_CART",
  "links": [
    {
      "label": "Ver carrito",
      "url": "/cart",
      "type": "CART"
    }
  ]
}
```

### Show invoice

No hay ruta frontend confirmada para facturas. El chatbot mantiene `SHOW_INVOICE` y entrega `invoiceNumber` y `saleId` en `context`/`metadata`, sin link visual inventado.

```json
{
  "uiAction": "SHOW_INVOICE",
  "links": [],
  "context": {
    "invoiceNumber": "FAC-0001",
    "saleId": "sale-001",
    "metadata": {
      "invoiceNumber": "FAC-0001",
      "saleId": "sale-001"
    }
  }
}
```

### General help suggestions

```json
{
  "context": {
    "intent": "general_help",
    "metadata": {
      "suggestions": [
        "Recomiendame ficcion",
        "Libros para aprender",
        "Algo para regalar",
        "Ver libros disponibles"
      ]
    }
  }
}
```

Las sugerencias son textos que el usuario puede enviar como mensaje. No prometen compras y no requieren login.

## Seguridad

- React mantiene el chat abierto y decide si navega.
- FastAPI no ejecuta navegacion real.
- FastAPI no usa rutas backend como links visuales.
- FastAPI no hace HTTP real, no escribe base de datos y no ejecuta mutaciones reales.
- Las acciones sensibles siguen usando `PENDING_CONFIRMATION`.
