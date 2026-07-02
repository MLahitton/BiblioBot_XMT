# BIBLIOBOT BACKEND PROMPT - FASE CORE DE NEGOCIO

Actua como un arquitecto backend senior especializado en C#, .NET 10, ASP.NET Core, Entity Framework Core, PostgreSQL, autenticacion JWT y autorizacion por permisos.

Estoy trabajando en el proyecto real:

**BiblioBot**

Ruta local:

```txt
C:\Users\mlahi\Desktop\BiblioBot
```

El rol activo de este prompt es:

**Manuel - Backend y reglas de negocio**

---

# CONTEXTO DEL PROYECTO

BiblioBot maneja tres capas de uso:

1. Cliente: consultas de libro y compra.
2. Trabajador: ingreso de inventario y operaciones internas.
3. Admin: consulta y administracion de usuarios, permisos, ventas y solicitudes.

Adicionalmente existe un chatbot con IA que debe usar el backend como unica fuente de verdad.

---

# OBJETIVO EXACTO DE ESTA FASE

Implementar en ASP.NET Core la API principal que soporte:

- Catalogo y busquedas de libros.
- Compra tipo ecommerce desde UI y desde chatbot.
- Registro atómico de ventas con ajuste de inventario.
- Registro y trazabilidad de entradas y salidas.
- Gestión de solicitudes de compras y traslados.
- Integracion con chatbot por endpoint interno.
- Consulta de facturas y reportes basicos.

No implementar procesamiento del LLM.

---

# REGLA FUNCIONAL PRINCIPAL

Una venta solo se concreta si:

- Existe libro activo.
- Cantidad solicitada <= stock disponible.
- Usuario autorizado.
- La transacción se aplica de forma atomica: inventario, venta, detalle, factura si aplica.

Si falla cualquiera de estas condiciones, la venta debe quedar rechazada y debe revertirse cualquier cambio parcial.

---

# ENDPOINTS MINIMOS A IMPLEMENTAR

## Chat y orquestacion

- `POST /api/chat/message`
  - Envia mensaje desde frontend a FastAPI via .NET.
  - Debe incluir identificador de sesion y usuario.

## Catalogo

- `GET /api/libros`
- `GET /api/libros/{id}`
- `GET /api/libros/search`

## Carrito y ventas

- `POST /api/carrito`
- `GET /api/carrito/{sessionId}`
- `POST /api/ventas`
- `POST /api/ventas/{id}/confirmar`
- `GET /api/ventas`

## Facturacion

- `GET /api/facturas/{id}`

## Inventario y operaciones internas

- `GET /api/inventario`
- `POST /api/inventario/entradas`
- `POST /api/inventario/salidas`

## Solicitudes internas

- `POST /api/solicitudes/compras`
- `POST /api/solicitudes/traslados`

## Admin

- `GET /api/admin/usuarios`
- `GET /api/admin/roles`
- `GET /api/admin/permisos`

---

# MODELO DE NEGOCIO OBLIGATORIO

Implementar entidades y relaciones minimas:

- Libro
- Usuario (cliente, trabajador, admin)
- Rol / Permiso
- Venta, DetalleVenta
- Factura
- InventarioMovimiento (entrada/salida/ajuste)
- Solicitud (compra/traslado)
- ChatLog/ConversationState opcional para trazabilidad.

Reglas criticas:

1. Venta debe registrar origen (`CHATBOT` o `WEB_UI`).
2. Debe haber rastro de `actorId`, fecha y `sede` (si aplica).
3. Solicitudes con estado: `CREADA`, `EN_REVISION`, `APROBADA`, `RECHAZADA`, `EJECUTADA`.
4. No hardcodear estados de dominio sin catálogo.
5. Si no hay stock suficiente, respuesta controlada sin cambiar estado.

---

# SEGURIDAD Y PERMISOS

1. JWT en todas las rutas protegidas.
2. Permisos por endpoint para:
   - compras por cliente/workflow.
   - operaciones de inventario.
   - consultas administrativas.
3. Permitir consultas de clientes sin privilegios administrativos.
4. Evitar privilegios por default.

---

# ARQUITECTURA TECNICA ESPERADA

1. Capa de aplicacion con casos de uso claros.
2. Capa de infraestructura para acceso a PostgreSQL.
3. Capa de API con validaciones DTO y mapeo.
4. Transacciones para operaciones que mezclan inventario + venta.
5. Logs de integración con FastAPI.
6. Idempotencia para confirmaciones repetidas.

---

# RESTRICCIONES DE ESTE ROL

1. No tocar frontend.
2. No implementar UI de chat.
3. No inventar contratos de endpoint que no existan; validar y respetar el contrato real.
4. No guardar datos internos en caché del cliente.
5. No crear migraciones o seeds salvo orden expreso.
6. No exponer datos sensibles.

---

# VALIDACION TECNICA REQUERIDA

1. Compilar con `dotnet build`.
2. Verificar tests existentes relevantes si aplica.
3. Reportar warnings o errores.
4. Confirmar que los endpoints devuelven codigos y cuerpos esperados.

---

# FORMATO DE RESPUESTA ESPERADA

REPORTE CODEX - BIBLIOBOT BACKEND CORE - MANUEL

1. Resultado general
   - APROBADO o NO APROBADO.
   - Breve motivo.
2. Archivos creados
   - Listar exacto.
3. Archivos modificados
   - Listar exacto.
4. Endpoints implementados
   - Ruta, metodo, permisos y descripcion de contrato.
5. Reglas de negocio aplicadas
   - Validacion de stock.
   - Atomicidad de venta.
   - Origen CHATBOT / UI.
   - Trazabilidad de inventario.
6. Modelos y persistencia
   - Entidades nuevas o ajustadas.
7. Seguridad y permisos
   - Permisos usados.
   - Confirmar no se crean permisos nuevos sin necesidad.
8. Integracion de chatbot
   - Flujo `POST /api/chat/message`.
   - manejo de errores de FastAPI.
9. Calidad tecnica
   - dotnet build ejecutado y resultado.
   - warnings reportados.
10. Riesgos o pendientes
   - puntos que queden para siguientes fases.
11. Confirmaciones obligatorias
   - No commit, no push, no deploy.
   - No modifica frontend.
