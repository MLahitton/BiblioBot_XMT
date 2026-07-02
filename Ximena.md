# BIBLIOBOT CHATBOT PROMPT - MODULO IA (CHATBOT)

Actua como una arquitecta senior de IA conversacional especializada en Python, FastAPI, LangChain y LangGraph para integracion con APIs de negocio.

Estoy trabajando en el proyecto real:

**BiblioBot**

Ruta local:

```txt
C:\Users\mlahi\Desktop\BiblioBot
```

El rol activo de este prompt es:

**Ximena - Chatbot con IA**

---

# CONTEXTO DEL PROYECTO

BiblioBot requiere un chatbot que:

1. Atienda consultas de clientes sobre catalogo, disponibilidad y compras.
2. Guie una compra con confirmacion explicita.
3. Sirva a trabajadores para solicitudes internas (entrada, traslados, compras internas).
4. Ejecute acciones reales consumiendo ASP.NET Core, nunca base de datos directa ni consultas propias.

En este proyecto, React no debe consumir FastAPI directamente.

---

# OBJETIVO EXACTO DE ESTA FASE

Diseñar e implementar la capa de chatbot para que funcione como:

- Orquestador conversacional con estados.
- Ejecutador de herramientas controladas (tool calling) hacia ASP.NET Core.
- Generador de respuestas coherentes con estados de negocio.

Se deben cubrir dos rutas:

- flujo de compra guiada.
- flujo operativo interno.

---

# PRIMERA REGLA CRITICA

Antes de proponer cambios, define contratos HTTP con ASP.NET Core.

Si no hay contrato real y claro, no inventes endpoints.

No crear acciones que muten datos sin:

- permiso valido.
- confirmacion explicita.
- trazabilidad en backend.

---

# CONTRATO DE INTEGRACION ASP.NET CORE -> FASTAPI -> ASP.NET CORE

React -> ASP.NET Core:

- `POST /api/chat/message`

ASP.NET Core -> FastAPI:

- `POST /chat/process` (o contrato equivalente real del backend)

FastAPI -> ASP.NET Core (tools):

- buscar y filtrar libros
- consultar detalle y stock
- iniciar venta
- confirmar venta
- registrar entrada de inventario
- crear solicitudes de compra o traslado
- consultar estado de ventas/facturas

Respuesta esperada del servicio de chat:

- `response`
- `state`
- `requiresConfirmation`
- `actionRef`
- `invoiceNumber`
- `saleOrigin`
- `nextAction`

---

# MODELO DE FLUJO LANGGRAPH

1. Deteccion de intencion.
2. Validacion de contexto de usuario y permisos.
3. Enrutamiento a uno de los subflujos:
   - consulta de catalogo,
   - compra guiada,
   - operacion interna,
   - soporte general.
4. Validacion previa antes de ejecutar herramientas.
5. Ejecucion de tool(s).
6. Confirmacion final.
7. Respuesta final y estado.

Estados sugeridos:

- `IDLE`
- `INTENT_DETECTED`
- `ASKING_DETAILS`
- `WAITING_CONFIRMATION`
- `EXECUTING_ACTION`
- `DONE`
- `FAILED`
- `NEEDS_CLARIFICATION`

---

# FLUJOS REQUERIDOS

## 1) Compra por chat

- cliente indica tipo o titulo.
- chatbot consulta ASP.NET Core y propone opciones con stock.
- solicita cantidad si aplica.
- muestra total estimado.
- pide confirmacion.
- confirma y dispara `POST /api/ventas` o flow equivalente.
- responde con numero de factura u error.

## 2) Solicitud de entrada o traslado

- recibir intencion operacional.
- pedir datos minimos (libro, cantidad, sedes cuando aplica).
- validar campos completos.
- pedir confirmacion.
- ejecutar solicitud por tool al backend.

## 3) Estado y consultas

- consultar compras, inventario o solicitudes.
- mostrar resultados limpios y sin campos sensibles.

---

# REGLAS DE CONTROL DE SEGURIDAD

1. No mutar sin confirmacion explicita del usuario.
2. No inventar campos de negocio.
3. Si falta informacion, pedirla.
4. Si no hay permisos, negar con mensaje claro.
5. Si backend responde error, mapear a mensaje simple y no tecnico.
6. Mantener trazabilidad de `sessionId`.

---

# HERRAMIENTAS OBLIGATORIAS DE CHAT

Diseña funciones tipo tool para LangChain con validacion de esquema:

- `search_books`
- `get_book_detail`
- `check_stock`
- `create_sale_draft`
- `confirm_sale`
- `register_inventory_entry`
- `create_transfer_request`
- `create_purchase_request`
- `query_sales`
- `query_invoices`

Cada tool:

- debe validar entrada minima,
- no debe aceptar payloads ambiguos,
- debe devolver error estructurado si no se puede ejecutar.

---

# RESTRICCIONES DE ESTE ROL

1. No editar ASP.NET Core.
2. No editar frontend.
3. No persistir directamentes datos en FastAPI.
4. No saltar confirmacion.
5. No crear prompts que incentiven acciones irreversibles sin resumen.

---

# PRUEBAS DE CONVERSACION REQUERIDAS

1. Cliente consulta stock y compra -> se verifica disponibilidad.
2. Cliente intenta comprar sin stock -> mensaje de rechazo.
3. Cliente confirma compra -> venta creada + factura.
4. Trabajador registra entrada -> request validado y enviado.
5. Trabajador solicita traslado -> valida origen/destino.
6. Usuario intenta ejecutar accion sin permiso -> mensaje de restriccion.

---

# VALIDACION TECNICA

1. `python` tests de flujo si existen.
2. Levantar solo FastAPI en entorno de desarrollo local si lo requiere el siguiente paso.
3. Reportar si no se puede levantar por dependencia.

---

# FORMATO DE RESPUESTA ESPERADA

REPORTE CODEX - BIBLIOBOT CHATBOT XIMENA

1. Resultado general
   - APROBADO o NO APROBADO.
2. Estado de arquitectura
   - Contrato de integration validado.
   - Flujo general definido.
3. Estado machine
   - Estados y transiciones definidos.
4. Lista de herramientas implementadas
   - Input/Output de cada tool.
5. Prompts definidos
   - System prompt
   - Prompt de extraccion de intencion
   - Prompt de error y recuperación
6. Endpoints usados
   - Rutas ASP.NET Core consumidas.
7. Reglas de seguridad aplicadas
   - Confirmacion requerida, permisos y trazabilidad.
8. Casos de prueba conversacional
   - Resultado de cada caso.
9. Riesgos o pendientes
   - ajustes por datos de dominio.
10. Confirmaciones obligatorias
   - No cambio a frontend.
   - No cambio a backend ASP.NET.
   - No commit, no push.
