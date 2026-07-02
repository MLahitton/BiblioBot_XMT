# BiblioBot - Especificacion Combinada de Negocio y Requerimientos Tecnicos

## 1) Vision y alcance

Este documento integra:

- Las reglas de negocio de la plataforma BiblioBot para biblioteca.
- Las guias tecnicas del taller de Inventario Inteligente con Chatbot de Ventas.

Objetivo integrado:

Construir una aplicacion web para biblioteca que gestione inventario, compras, traslados, ventas y consultas, incorporando un chatbot que permite ejecutar acciones internas del negocio mediante conversaciones y que se comunica de forma controlada con el backend.

El frontend tambien debe cubrir flujos de ecommerce (clientes) y de operación interna (trabajadores), además del chat.

## 2) Alcance funcional del negocio (BiblioBot)

### 2.1 Usuario final: clientes

- Consultar libros disponibles.
- Consultar libros por genero, autor, titulo, editorial, categoria, fecha y estado.
- Comprar libros desde interfaz y/o chatbot.
- Ver historial de compras.

### 2.2 Usuarios operativos: trabajadores

- Registrar entrada de inventario.
- Registrar ventas.
- Registrar solicitudes de compras.
- Registrar y solicitar traslados entre sedes.
- Consultar estado de inventario y solicitudes.

### 2.3 Usuarios administrativos

- Consultar usuarios, roles y permisos.
- Consultar bibliotecas, clientes y trabajadores.
- Consultar solicitudes de traslado y compra.
- Consultar inventario y ventas.

### 2.4 Reglas de negocio clave

- La accion del chatbot debe mapearse a operaciones de negocio reales.
- Toda accion ejecutada por chatbot debe validar permisos del usuario.
- Una compra debe verificar disponibilidad antes de confirmarla.
- Una compra confirmada debe:
  - registrar venta,
  - descontar inventario de forma transaccional,
  - marcar origen de venta como CHATBOT (o equivalente),
  - emitir informacion de factura.
- Las altas o ajustes de inventario y traslados deben registrar trazabilidad (quien, cuando, que accion).
- Las solicitudes internas (compras, traslados) deben quedar con ciclo de estado.

### 2.5 Acciones disponibles en frontend (ademas del chatbot)

- Clientes: comprar desde interfaz tipo ecommerce (catálogo, carrito, checkout y seguimiento de ordenes).
- Trabajadores: registrar ventas internas, entradas y ajustes de inventario, y gestionar solicitudes de compra/traslado desde vistas operativas.
- El chatbot y la UI pueden ejecutar los mismos procesos con distintas experiencias de usuario.

## 3) Requerimientos tecnicos de implementacion

### 3.1 Stack principal

- Frontend: React con TypeScript.
- API de negocio: ASP.NET Core + Entity Framework Core.
- API de chatbot: FastAPI (Python).
- Agente: LangChain + LangGraph.
- Base de datos: PostgreSQL.
- Busquedas semanticas: pgvector.

### 3.2 Restriccion de integracion obligatoria

- React consume unicamente endpoints de ASP.NET Core.
- ASP.NET Core actua como API principal y punto unico de ingreso.
- ASP.NET Core consume FastAPI.
- FastAPI no realiza operaciones criticas de negocio por si misma.

El frontend debe exponer:

- Vistas de ecommerce para clientes (busqueda, carrito y compra).
- Vistas de inventario y transacciones para trabajadores (entrada, salida/venta interna, solicitudes).

## 4) Arquitectura objetivo unificada

1. React (frontend)
   - Interfaz de clientes, trabajadores y admin.
   - UI de chatbot y administracion.
   - Flujos ecommerce para clientes (catálogo, carrito, compra y pago).
   - Flujos operativos de inventario/ventas para trabajadores.

2. ASP.NET Core (Backend principal)
   - Reglas de negocio, persistencia y seguridad.
   - Orquesta llamadas a la API del chatbot.
   - Expone endpoints para catalogo, inventario, ventas, usuarios, solicitudes y chat.

3. FastAPI (servicio de conversacion)
   - Recibe mensajes de .NET.
   - Orquesta estados conversacionales con LangGraph.
   - Usa herramientas con LangChain para llamar de vuelta a ASP.NET Core.

4. PostgreSQL + pgvector
   - Catálogo, movimientos, usuarios, ventas, facturas y trazabilidad.
   - pgvector para recomendaciones o busquedas por semantica si aplica.

### Diagrama de flujo de mensaje

- Usuario -> React -> ASP.NET Core -> FastAPI -> LangGraph/LangChain -> ASP.NET Core -> Postgres -> ASP.NET Core -> FastAPI -> ASP.NET Core -> React

## 5) Casos de uso combinados y flujo funcional

### 5.1 Compra por cliente

- El cliente consulta o pide un libro en chatbot.
- Chatbot valida disponibilidad a traves de ASP.NET Core.
- El sistema pide confirmacion antes de ejecutar venta.
- Al confirmar:
  - crear venta,
  - descontar stock,
  - generar factura,
  - devolver numero de factura y estado final al cliente.

### 5.2 Solicitud y entrada de inventario por trabajador

- El trabajador solicita registro de entrada (ej: `llegaron 50 ejemplares ...`).
- Chatbot interpreta y pide confirmacion.
- ASP.NET Core registra movimiento de entrada y actualiza inventario.
- Respuesta de confirmacion al usuario.

### 5.3 Solicitud de traslado entre sedes

- Trabajador solicita traslado indicando titulo, cantidad y sedes origen/destino.
- Chatbot valida que la solicitud cumple estructura minima.
- ASP.NET Core crea solicitud y permite su seguimiento.
- Backend marca estado y auditoria de aprobacion/ejecucion.

### 5.4 Consulta administrativa

- Admin consulta usuarios, roles, permisos, clientes, trabajadores, inventario, ventas y solicitudes desde React.
- React hace llamadas al backend .NET, sin acceso directo a FastAPI.

### 5.5 Compra y venta por interfaz web (ecommerce / inventario interno)

- Cliente usa la UI para:
  - navegar catálogo,
  - armar carrito,
  - confirmar compra (mismo flujo de validación de stock que el chat),
  - y consultar estado/ facturación.
- Trabajador usa la UI interna para:
  - registrar ventas operativas,
  - registrar entradas/salidas,
  - y consultar movimientos de inventario.
- Este canal funciona completo incluso si el usuario no interactúa con el chatbot.

## 6) Crosswalk: regla de negocio <-> componente tecnico

| Regla de negocio | Componente tecnico principal | Observaciones |
| --- | --- | --- |
| Registro de venta con actualizacion de stock | ASP.NET Core + EF Core + Postgres | Operacion transaccional y valida estado de inventario |
| Compra ecommerce en UI de clientes | React + ASP.NET Core | Mantiene reglas de stock, checkout y facturacion sin pasar por chat |
| Compra asistida por chat | React -> ASP.NET Core -> FastAPI -> ASP.NET Core | Mantener estado conversacional controlado |
| Entrada de inventario | ASP.NET Core + endpoints de movimientos | Chatbot invoca endpoints existentes, no escribe directamente |
| Venta operativa interna de trabajador | ASP.NET Core + React | UI dedicada para ajuste rápido y trazabilidad por usuario |
| Solicitudes de compra y traslado | ASP.NET Core + historial de solicitudes | Estados: creada, en revision, aprobada, rechazada, ejecutada |
| Busqueda por catalogo y atributos | ASP.NET Core + Postgres | Filtros por genero, autor, titulo, editorial, fecha, estado |
| Uso de chatbot para tareas internas | FastAPI + LangChain + LangGraph | Herramientas restringidas por tipo de usuario |
| Dashboard y control operacional | React + ASP.NET Core | Metricas para admin: stock bajo, ventas, facturacion y origen de venta |

## 7) Propuesta de endpoints basicos

### 7.1 .NET (expuesto a React)

- `POST /api/chat/message` : enviar mensaje al chatbot y recibir respuesta.
- `GET /api/libros` : listar catalogo.
- `GET /api/libros/{id}` : detalle de libro.
- `GET /api/libros/search` : búsqueda y filtros avanzados de catálogo.
- `POST /api/ventas` : crear venta.
- `GET /api/ventas` : listar ventas (filtros por origen, fecha, usuario).
- `POST /api/ventas/{id}/confirmar` : confirmar venta desde UI.
- `POST /api/carrito` : crear o actualizar carrito.
- `GET /api/carrito/{sessionId}` : consultar carrito por sesión.
- `POST /api/inventario/entradas` : registrar entrada.
- `POST /api/inventario/salidas` : registrar salida o venta operativa interna.
- `POST /api/solicitudes/traslados` : crear solicitud de traslado.
- `POST /api/solicitudes/compras` : crear solicitud de compra.
- `GET /api/inventario` : consultar inventario.
- `GET /api/admin/usuarios` : consultas de usuarios y permisos.
- `GET /api/facturas/{id}` : consulta de factura.

### 7.2 FastAPI (consumido por .NET)

- `POST /chat/process` : recibe `sessionId` y `message`.
- `GET /health` : monitoreo basico del servicio.

## 8) Requisitos de datos y modelos sugeridos

- `Libro`: titulo, autor, editorial, categoria, genero, anoPublicacion, estado, precio, stockActual.
- `InventarioMovimiento`: tipo(entrada, ajuste, salida), cantidad, usuarioId, libroId, sedeId, observacion, fecha.
- `Venta`: clienteId, origen(Chatbot/Manual), estado, total, fecha, invoiceId.
- `DetalleVenta`: libroId, cantidad, precioUnitario, subtotal.
- `Factura`: ventaId, numeroFactura, fecha, total.
- `Solicitud`: tipo(compra, traslado), estado, origenUsuario, datosDeOrigen, destino, cantidad, libroId.
- `Usuario`: rol(cliente, trabajador, admin), permisos, sede (opcional).

## 9) Requisitos no funcionales

- Seguridad:
  - Autenticacion y autorizacion por rol.
  - Cada accion del chatbot validada contra permisos.
- Observabilidad:
  - Trazabilidad de acciones del chatbot.
  - Logs de llamadas entre .NET y FastAPI.
- Confiabilidad:
  - Operaciones criticas de inventario y ventas en transacciones.
  - Idempotencia para mensajes repetidos de confirmacion.
- Rendimiento:
  - Filtros y paginacion en listados de inventario/ventas.
  - Indices en consultas frecuentes (sku, titulo, estado, fecha, usuario).

## 10) Criterios de aceptacion unificados

- El cliente puede comprar mediante chat y formulario con flujo de confirmacion.
- La compra solo finaliza si hay stock y confirmacion explicita.
- La venta queda registrada y se reduce stock.
- El frontend permite al cliente completar compras con experiencia ecommerce (búsqueda, carrito, checkout y seguimiento).
- El frontend permite al trabajador gestionar ventas internas y movimientos de inventario (entrada/salida) desde interfaz.
- La factura se puede consultar tras la compra.
- El trabajador puede registrar entradas y solicitar traslados mediante chat o UI.
- React no consume FastAPI de forma directa.
- Se mantienen registros de usuarios, roles y permisos con filtrado por vista administrativa.

## 11) Entregables sugeridos

1. API de negocio en ASP.NET Core completa con reglas de negocio.
2. Servicio de chatbot en FastAPI con LangChain/LangGraph.
3. Frontend React con dashboard, catalogo, inventario, ventas, facturas y chat.
4. Integracion React -> ASP.NET Core -> FastAPI -> ASP.NET Core funcionando end-to-end.
5. Evidencia de flujos de prueba de compra, solicitud y registro de inventario.

## 12) Alcance de esta combinacion

Este documento no reemplaza ninguno de los documentos fuente; los unifica para dar una hoja de ruta ejecutable de lo que el negocio necesita y como lo soporta la arquitectura tecnica del taller.
