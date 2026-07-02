# BIBLIOBOT FRONTEND PROMPT - FASE FRONTEND CORE

Actua como un arquitecto frontend senior especializado en React, TypeScript, Vite, React Router, TanStack Query, React Hook Form y consumo seguro de APIs REST con JWT.

Estoy trabajando en el proyecto real:

**BiblioBot**

Ruta local:

```txt
C:\Users\mlahi\Desktop\BiblioBot
```

El rol activo de este prompt es:

**Tomas - Frontend**

---

# CONTEXTO ACTUAL

Este proyecto ya combina:

1. Venta de libros para clientes por interfaz (ecommerce).
2. Gestion operativa de inventario para trabajadores internos.
3. Consulta de usuarios, permisos y reportes para administradores.
4. Chatbot integrado para apoyar compras y operaciones (solo por backend).

Se debe mantener la regla arquitectonica principal:

- React consume solo ASP.NET Core.
- ASP.NET Core consume FastAPI para chatbot.
- FastAPI no es consumido por React.

El proyecto actual espera que el frontend permita dos lineas funcionales:

- Experiencia de cliente (catalogo, compra, seguimiento).
- Flujo operativo para trabajadores (inventario, ventas internas, solicitudes).

---

# IDEA FUNCIONAL DE ESTA FASE

Implementar y/o completar el frontend de BiblioBot para que cubra, sin ambiguedad, los 2 modos de uso:

1. **Cliente**: comprar libros como ecommerce.
2. **Trabajador**: registrar entrada/salida del inventario y consultas operativas.

Todo debe quedar ejecutable sin depender exclusivamente de chatbot.

---

# OBJETIVO DE ESTA FASE

Entregar un frontend con:

1. Rutas navegables para:
   - Catalogo y detalle de libro.
   - Carrito y checkout.
   - Historial de compras.
   - Dashboard de inventario por trabajador.
   - Registro de entradas y salidas internas.
   - Bandeja y seguimiento de solicitudes.
   - Modulo de chat para conversar con backend via .NET.
2. Servicios tipados para consumir endpoints de ASP.NET Core.
3. Formularios con validacion clara y manejo de estados.
4. Proteccion por permisos para mostrar u ocultar acciones.

No se debe implementar logica de negocio crtica en frontend.

---

# PRIMERA REGLA CRITICA

Antes de editar cualquier archivo, confirma que el contexto base esta en:

```txt
C:\Users\mlahi\Desktop\BiblioBot
```

Si no estas en esta carpeta, debes detenerte y reportar:

```txt
ERROR DE CONTEXTO: Este prompt no esta ubicado en BiblioBot. No se realizaron cambios.
```

---

# CONTRATOS DE API (ASP.NET CORE)

Usar endpoints reales confirmados por backend:

- `GET /api/libros`
- `GET /api/libros/{id}`
- `GET /api/libros/search`
- `GET /api/inventario`
- `POST /api/inventario/entradas`
- `POST /api/inventario/salidas`
- `POST /api/inventario/salidas` (ventas internas o movimiento de salida)
- `POST /api/inventario/ajustes` (si existiera endpoint real de ajuste)
- `POST /api/carrito`
- `GET /api/carrito/{sessionId}`
- `POST /api/ventas`
- `POST /api/ventas/{id}/confirmar`
- `GET /api/ventas`
- `GET /api/facturas/{id}`
- `POST /api/solicitudes/compras`
- `POST /api/solicitudes/traslados`
- `POST /api/chat/message`

Regla clave:

- No llamar a FastAPI desde React.
- Cada pantalla debe manejar errores propios del endpoint y mostrar mensajes amigables.

---

# ARQUITECTURA FRONTEND DESEADA

1. Estado global por contexto:
   - Auth + permisos.
   - Sesion de chat y carrito.
   - Filtros de catalogo y resultados.

2. Estructura por dominio:
   - `features/catalog` (filtros, detalle, listados, carrito).
   - `features/sales` (ventas, facturas, confirmaciones).
   - `features/inventory` (entradas, salidas, solicitudes).
   - `features/chatbot` (conversaciones).
   - `features/admin` (usuarios, permisos, reportes).
   - `features/common` (api client, auth guard, types).

3. UI guidelines:
   - Navegacion por rol: cliente / trabajador / admin.
   - Estados de carga y vacio visibles.
   - Deshabilitar botones de accion critica mientras se envia.

---

# TAREAS ESPECIFICAS DE TOMAS

## 1) Flujo ecommerce cliente

- Catalogo con busquedas por titulo, autor, genero, editorial, categoria y fecha.
- Detalle de libro con disponibilidad.
- Carrito: agregar, quitar, cambiar cantidad, total.
- Checkout:
  - revisar autenticacion.
  - confirmar datos.
  - enviar venta via endpoint.
  - mostrar estado de venta y factura.

## 2) Flujo operativo trabajador

- Pantalla de entradas de inventario.
- Pantalla de salidas/ventas internas.
- Registro de solicitudes de compra y traslado con campos de trazabilidad.
- Listados con filtro por estado.

## 3) Chatbot UI

- Chat panel sencillo para enviar mensajes.
- Mostrar respuestas y estado (`WAITING_CONFIRMATION`, `DONE`, `ERROR`).
- Mostrar resumen de acciones ejecutadas por chat.

## 4) Permisos y seguridad UI

- El boton/accion solo aparece si el usuario tiene permiso correspondiente.
- No esconder funciones por rol solo de texto, usar matriz de permisos del token/me.

---

# RESTRICCIONES

1. No tocar backend en esta fase.
2. No exponer datos sensibles en cliente.
3. No hacer llamadas directas a PostgreSQL ni a FastAPI.
4. No loguear tokens ni contraseñas ni request bodies sensibles.
5. No crear endpoints nuevos no aprobados.
6. No crear archivos de config o dependencias nuevas sin pedirlo.
7. Mantener consistencia tipografica y de rutas del proyecto existente.

---

# VALIDACION FUNCIONAL ESPERADA

Antes de terminar, verifica o planifica pruebas manuales de:

1. Usuario cliente puede buscar y comprar desde UI sin usar chat.
2. Usuario trabajador puede registrar entrada y salida en inventario.
3. Chatbot responde desde la UI sin que React lo consuma por fuera de .NET.
4. Rutas protegidas por permiso funcionan.
5. Estados de espera de respuesta, error y exito visibles.

---

# FORMATO DE RESPUESTA ESPERADA

REPORTE CODEX - BIBLIOBOT FRONTEND TOMAS

1. Resultado general
   - Fase completada o con bloqueos.
2. Contexto validado
   - Carpeta trabajada.
   - Confirmacion de consumo solo a ASP.NET Core.
   - Confirmacion de no cambio en backend/FastAPI.
3. Arquitectura frontend creada
   - Estructura de rutas y dominios.
   - Estrategia de estado.
4. Contratos usados
   - Lista de endpoints usados.
   - Campos que se enviaron y recibieron.
5. Archivos creados
   - Enumerar ruta y proposito.
6. Archivos modificados
   - Enumerar ruta y proposito.
7. Casos funcionales implementados
   - Catalogo y detalle.
   - Checkout ecommerce.
   - Operaciones de inventario.
   - Chatbot UI.
   - Permisos de accion.
8. Seguridad frontend
   - Confirmar no logs sensibles.
   - Confirmar control por permisos.
9. Validacion tecnica
   - build/lint (si aplica) y estado.
10. Pendientes para siguiente fase
   - Mejora UX, tests e2e, ajustes de performance.
11. Confirmaciones obligatorias
   - No commit, no push, no deploy.
   - No cambios en backend.
