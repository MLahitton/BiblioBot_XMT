# Fase 11 - Flujo de Chat para GUEST

## Que es GUEST

`GUEST` representa un usuario invitado controlado por ASP.NET Core. FastAPI no valida JWT ni crea identidad propia; recibe `roles`, `permissions`, `userId` y `userEmail` ya definidos por el backend.

## Contrato de entrada

Un invitado puede llegar con:

- `userId: null`
- `userEmail: null`
- `roles: ["GUEST"]`
- `permissions: ["chat.message", "books.read", "books.search"]`

`userId` tambien acepta UUID para usuarios autenticados. `message`, `roles` y `permissions` siguen siendo obligatorios.

## Que puede hacer

Un invitado puede usar el chat para consultas publicas segun permisos explicitos:

- Buscar libros.
- Pedir recomendaciones.
- Ver catalogo.
- Ver detalle de libro.
- Consultar disponibilidad basica si tiene `books.read`.

El rol `GUEST` no otorga permisos por si mismo.

## Que no puede hacer

Un invitado no puede ejecutar acciones protegidas:

- Agregar al carrito o preparar compras.
- Consultar facturas o ventas.
- Consultar inventario administrativo.
- Registrar movimientos de inventario.
- Crear solicitudes internas de compra o traslado.
- Ver reportes o funciones administrativas.

## AUTH_REQUIRED

Si un invitado intenta una accion protegida, el grafo responde `NEEDS_CLARIFICATION` con `context.intent = "auth_required"` y `context.nextAction = "AUTH_REQUIRED"`.

La respuesta incluye links seguros:

- `/auth/login` con type `AUTH_LOGIN`
- `/auth/register` con type `AUTH_REGISTER`

`uiAction` se mantiene en `NONE` porque no existe una accion visual nueva para login o registro en esta fase.

## Responsabilidades

FastAPI solo devuelve texto, links y metadata. React decide si navega a login o registro y mantiene el chat abierto.

FastAPI no debe hacer HTTP real hacia ASP.NET Core, no debe escribir en base de datos ni debe ejecutar mutaciones reales.
