Actúa como un arquitecto backend senior especializado en C#, .NET 10, ASP.NET Core, Clean Architecture, Vertical Slices, Modular Monolith, Entity Framework Core, PostgreSQL, MediatR, JWT, authorization policies, RBAC, APIs REST seguras, ecommerce, ventas atómicas, control de inventario, facturación, idempotencia y consistencia transaccional.

Estoy trabajando en el proyecto real:

BiblioBot Backend

Ruta local:

C:\Users\mlahi\Desktop\BiblioBot\bibliobot_Backend

Solución:

BiblioBot.sln

Repositorio:

https://github.com/MLahitton/BiblioBot_XMT

Carpeta del backend dentro del repo:

bibliobot_Backend

============================================================
CONTEXTO GENERAL DEL SISTEMA
============================

BiblioBot es un backend ASP.NET Core para gestión de biblioteca/librería con catálogo de libros, búsquedas, carrito, compras tipo ecommerce, ventas, facturación, inventario, solicitudes internas, usuarios, roles, permisos e integración posterior con chatbot.

El backend ASP.NET Core será la única fuente de verdad para:

* Catálogo.
* Stock.
* Carrito.
* Ventas.
* Facturas.
* Inventario.
* Usuarios.
* Roles.
* Permisos.
* Solicitudes.
* Trazabilidad básica de chat.

La arquitectura aprobada es:

Clean Architecture + Vertical Slices + Hexagonal ligero.

El sistema será un Modular Monolith.

La base de datos aprobada es PostgreSQL.

La persistencia se maneja con Entity Framework Core.

============================================================
ESTADO ACTUAL
=============

Ya existe:

* Modelo de dominio inicial.
* BiblioBotDbContext.
* Migración InitialCreate aplicada.
* Seeder técnico ejecutado.
* Auth Core:

  * POST /api/auth/register
  * POST /api/auth/login
  * POST /api/auth/refresh
  * GET /api/auth/me
* JWT con claims de roles y permisos.
* Permission policies registradas.
* Books Read API.
* Books Management API.
* Catalog Support API:

  * Autores.
  * Categorías.
  * Editoriales.
* Branches Management API.
* Inventory Core API:

  * GET /api/inventario
  * GET /api/inventario/movimientos
  * POST /api/inventario/entradas
  * POST /api/inventario/salidas
  * POST /api/inventario/ajustes
* Cart Core API:

  * POST /api/carrito
  * GET /api/carrito/{sessionId}
  * DELETE /api/carrito/{sessionId}/items/{bookId}
  * DELETE /api/carrito/{sessionId}

Ya existen entidades:

* User
* Book
* Cart
* CartItem
* Sale
* SaleDetail
* SaleStatus
* SaleOrigin
* Invoice
* InventoryStock
* InventoryMovement
* InventoryMovementType
* Branch

Ya existen constantes:

* SaleStatusCodes.Created

* SaleStatusCodes.PendingConfirmation

* SaleStatusCodes.Confirmed

* SaleStatusCodes.Rejected

* SaleStatusCodes.Cancelled

* SaleOriginCodes.WebUi

* SaleOriginCodes.Chatbot

* InventoryMovementTypeCodes.Sale

* CartStatusCodes.Active

* CartStatusCodes.CheckedOut

* CartStatusCodes.Cancelled

Ya existen permisos relevantes:

* sales.create
* sales.confirm
* sales.read_own
* sales.read_all
* invoices.read_own
* invoices.read_all

Build actual limpio:

* 0 errores.
* 0 warnings.

Todavía NO existe:

* API de ventas.
* Venta preliminar desde carrito.
* Confirmación atómica de venta.
* Descuento definitivo de inventario por venta.
* Movimiento de inventario tipo SALE desde venta.
* Generación de factura.
* Consulta de facturas.

============================================================
OBJETIVO EXACTO DE ESTA FASE
============================

Implementar el núcleo de ventas del backend.

Esta fase debe crear:

* POST /api/ventas
* POST /api/ventas/{id}/confirmar
* GET /api/ventas
* GET /api/ventas/{id}

Estos endpoints deben:

* Usar MediatR.
* Usar IApplicationDbContext.
* Usar ICurrentUserService.
* Usar DTOs seguros.
* Usar policies por permiso.
* Crear ventas preliminares desde carrito.
* Confirmar ventas de forma atómica.
* Validar stock actual al confirmar.
* Descontar inventario solo al confirmar.
* Registrar movimientos de inventario tipo SALE.
* Generar factura al confirmar.
* Marcar carrito como CHECKED_OUT al confirmar.
* Evitar doble descuento de inventario.
* Mantener idempotencia de confirmación.
* Compilar con 0 errores y 0 warnings.

Esta fase NO debe implementar pagos.

Esta fase NO debe integrar pasarela de pagos.

Esta fase NO debe implementar envíos.

Esta fase NO debe implementar PDF de factura.

Esta fase NO debe implementar endpoints de facturas todavía.

Esta fase NO debe modificar frontend.

============================================================
REGLA FUNCIONAL PRINCIPAL
=========================

Una venta solo se concreta si:

* La venta existe.
* La venta no está confirmada previamente.
* El usuario está autenticado.
* El usuario tiene permiso suficiente.
* Todos los libros existen.
* Todos los libros están activos y no eliminados.
* Existe stock suficiente en la sede seleccionada.
* La operación completa se ejecuta de forma atómica.
* Se descuenta inventario.
* Se registra movimiento de inventario.
* Se marca venta como confirmada.
* Se genera factura.

Si falla cualquier condición:

* No se debe descontar inventario.
* No se debe confirmar la venta.
* No se debe generar factura.
* No se debe marcar el carrito como CHECKED_OUT.
* No deben quedar cambios parciales.
* Debe devolverse error controlado.

============================================================
ENDPOINTS A IMPLEMENTAR
=======================

1. POST /api/ventas

Policy requerida:

PermissionCodes.SalesCreate

Objetivo:

Crear una venta preliminar desde un carrito activo.

Body:

{
"sessionId": "string",
"branchId": "guid opcional",
"originCode": "WEB_UI"
}

Reglas:

* Usuario autenticado requerido.
* actorId debe salir del JWT por ICurrentUserService.
* customerId debe ser el usuario autenticado.
* sessionId requerido.
* sessionId máximo 120.
* branchId opcional.
* originCode requerido.
* originCode permitido:

  * WEB_UI
  * CHATBOT
* En esta fase, si originCode viene null o vacío, usar WEB_UI.
* Buscar carrito ACTIVE por sessionId.
* El carrito debe existir.
* El carrito debe tener al menos un item.
* Si el carrito tiene UserId y no coincide con el usuario autenticado, devolver 403 o error controlado.
* Si branchId viene:

  * La sede debe existir y estar activa.
* Buscar estado de venta CREATED o PENDING_CONFIRMATION.
* Preferir PENDING_CONFIRMATION si existe.
* Buscar SaleOrigin por originCode.
* Si no existe SaleOrigin, devolver error controlado.
* Crear Sale preliminar.
* Crear SaleDetails desde CartItems.
* Congelar snapshot:

  * BookTitleSnapshot.
  * IsbnSnapshot.
  * UnitPrice actual desde CartItem.UnitPrice.
  * Quantity.
  * LineTotal.
* Calcular:

  * Subtotal.
  * TaxTotal = 0 en esta fase.
  * Total = Subtotal.
* No descontar inventario.
* No crear factura.
* No crear movimientos de inventario.
* Guardar cambios.
* Retornar venta creada.

Respuestas esperadas:

* 201 Created.
* 400 Bad Request por validación.
* 401 Unauthorized si no autenticado.
* 403 Forbidden si no tiene permiso o carrito pertenece a otro usuario.
* 404 Not Found si carrito, sede, estado u origen no existe.
* 409 Conflict si carrito está vacío o no está activo.

2. POST /api/ventas/{id}/confirmar

Policy requerida:

PermissionCodes.SalesConfirm

Objetivo:

Confirmar una venta preliminar de forma atómica.

Reglas:

* Usuario autenticado requerido.
* actorId debe salir del JWT por ICurrentUserService.
* Venta debe existir.
* Venta debe tener detalles.
* Si venta ya está CONFIRMED:

  * No descontar inventario otra vez.
  * No crear movimientos duplicados.
  * No crear factura duplicada.
  * Retornar 200 OK con venta actual y flag de idempotencia si el DTO lo soporta.
* Si venta está CANCELLED o REJECTED:

  * Retornar 409 Conflict.
* Validar stock actual para cada detalle.
* La venta debe tener BranchId.
* Si la venta no tiene BranchId:

  * Retornar 400 Bad Request con mensaje claro.
  * En esta fase la confirmación requiere sede.
* Para cada SaleDetail:

  * Buscar InventoryStock por BookId + BranchId.
  * Validar CurrentStock >= Quantity.
* Si algún libro no tiene stock suficiente:

  * No modificar nada.
  * Retornar 409 Conflict.
* Buscar InventoryMovementType SALE.
* Buscar SaleStatus CONFIRMED.
* Descontar inventario por cada detalle.
* Registrar InventoryMovement por cada detalle:

  * BookId.
  * BranchId.
  * MovementTypeId SALE.
  * Quantity.
  * PreviousStock.
  * NewStock.
  * Reason: "Venta confirmada".
  * SaleId.
  * ActorId.
  * CreatedAt.
* Actualizar InventoryStock.CurrentStock.
* Actualizar InventoryStock.UpdatedAt.
* Cambiar Sale.StatusId a CONFIRMED.
* Sale.ConfirmedAt = DateTimeOffset.UtcNow.
* Actualizar Sale.UpdatedAt.
* Crear Invoice si no existe.
* InvoiceNumber debe ser determinístico/seguro para esta fase:

  * usar formato "FAC-" + fecha UTC yyyyMMddHHmmss + "-" + primeros 8 caracteres del SaleId sin guiones
  * debe ser único.
* Invoice:

  * SaleId.
  * CustomerId.
  * Subtotal.
  * TaxTotal.
  * Total.
  * IssuedAt.
  * IsCancelled = false.
* Si existe carrito asociado por SessionId en venta no está en modelo Sale.

  * Como Sale no tiene SessionId en el modelo actual, NO marcar carrito como CHECKED_OUT en esta fase salvo que el command reciba sessionId o se haya guardado alguna relación.
  * No modificar modelo ni migrar.
  * Reportar como pendiente: asociar Sale con Cart/Session en fase futura si es necesario.
* Guardar cambios de forma atómica.
* Usar transacción explícita en el handler con DbContext si está disponible por casting seguro al contexto real.
* Si no se puede usar transacción desde IApplicationDbContext sin tocar interfaces, usar una sola llamada SaveChangesAsync para atomicidad básica del DbContext y reportar pendiente de UnitOfWork/transacción formal.
* No dejar cambios parciales.

Respuestas esperadas:

* 200 OK.
* 400 Bad Request por venta sin sede o datos inválidos.
* 401 Unauthorized si no autenticado.
* 403 Forbidden si no tiene permiso.
* 404 Not Found si venta, stock, estado o tipo de movimiento no existe.
* 409 Conflict si stock insuficiente o estado inválido.

3. GET /api/ventas

Policy requerida:

Puede aceptar cualquiera de:

* PermissionCodes.SalesReadOwn
* PermissionCodes.SalesReadAll

Objetivo:

Consultar ventas.

Query params:

* pageNumber opcional, default 1.
* pageSize opcional, default 20.
* statusCode opcional.
* originCode opcional.
* customerId opcional.

Reglas:

* Usuario autenticado requerido.
* pageNumber mínimo 1.
* pageSize mínimo 1.
* pageSize máximo 100.
* Si usuario tiene sales.read_all:

  * Puede ver todas las ventas.
  * Puede filtrar por customerId.
* Si usuario solo tiene sales.read_own:

  * Solo puede ver sus ventas.
  * Ignorar customerId o rechazar si intenta consultar otro usuario.
* Ordenar por CreatedAt descendente.
* Usar AsNoTracking.
* Retornar paginado.
* No exponer entidades.

Respuestas:

* 200 OK.
* 401 Unauthorized.
* 403 Forbidden si no tiene sales.read_own ni sales.read_all.

4. GET /api/ventas/{id}

Policy requerida:

Puede aceptar cualquiera de:

* PermissionCodes.SalesReadOwn
* PermissionCodes.SalesReadAll

Objetivo:

Consultar detalle de una venta.

Reglas:

* Usuario autenticado requerido.
* Si tiene sales.read_all puede ver cualquier venta.
* Si solo tiene sales.read_own solo puede ver sus ventas.
* Si no existe, retornar 404.
* No exponer entidades.
* Incluir detalles.
* Incluir factura si existe.
* Incluir estado y origen.

Respuestas:

* 200 OK.
* 401 Unauthorized.
* 403 Forbidden si intenta ver venta ajena sin permiso.
* 404 Not Found.

============================================================
ARQUITECTURA ESPERADA
=====================

Usar Vertical Slices dentro de:

Application/Features/Sales

Crear casos de uso separados:

* CreateSale
* ConfirmSale
* GetSales
* GetSaleById

No crear repository en esta fase.

No crear servicios genéricos pesados.

No modificar Domain.

No modificar Infrastructure.

No meter lógica pesada en el controller.

============================================================
ARCHIVOS QUE DEBES CREAR
========================

Crear exactamente estos archivos si no existen:

Application/Features/Sales/Common/SaleDto.cs
Application/Features/Sales/Common/SaleDetailDto.cs
Application/Features/Sales/Common/SaleInvoiceDto.cs

Application/Features/Sales/CreateSale/CreateSaleCommand.cs
Application/Features/Sales/CreateSale/CreateSaleCommandHandler.cs

Application/Features/Sales/ConfirmSale/ConfirmSaleCommand.cs
Application/Features/Sales/ConfirmSale/ConfirmSaleCommandHandler.cs

Application/Features/Sales/GetSales/GetSalesQuery.cs
Application/Features/Sales/GetSales/GetSalesQueryHandler.cs

Application/Features/Sales/GetSaleById/GetSaleByIdQuery.cs
Application/Features/Sales/GetSaleById/GetSaleByIdQueryHandler.cs

Api/Contracts/Sales/CreateSaleRequest.cs

Api/Controllers/SalesController.cs

============================================================
CARPETAS QUE PUEDES CREAR
=========================

Puedes crear únicamente estas carpetas si no existen:

Application/Features/Sales
Application/Features/Sales/Common
Application/Features/Sales/CreateSale
Application/Features/Sales/ConfirmSale
Application/Features/Sales/GetSales
Application/Features/Sales/GetSaleById

Api/Contracts/Sales

============================================================
ARCHIVOS QUE PUEDES MODIFICAR
=============================

Puedes modificar únicamente archivos dentro de:

Application/Features/Sales/*
Api/Contracts/Sales/*
Api/Controllers/SalesController.cs

Solo si es necesario para crear o corregir esta fase.

============================================================
ARCHIVOS QUE NO DEBES TOCAR
===========================

No modifiques:

Domain/*
Infrastructure/*
Application/DependencyInjection.cs
Application/Common/*
Application/Features/Auth/*
Application/Features/Books/*
Application/Features/Catalog/*
Application/Features/Branches/*
Application/Features/Inventory/*
Application/Features/Cart/*
Api/Program.cs
Api/Controllers/AuthController.cs
Api/Controllers/BooksController.cs
Api/Controllers/AuthorsController.cs
Api/Controllers/CategoriesController.cs
Api/Controllers/PublishersController.cs
Api/Controllers/BranchesController.cs
Api/Controllers/InventoryController.cs
Api/Controllers/CartController.cs
Api/appsettings.json
Api/appsettings.Development.json
Api/Api.csproj
Application/Application.csproj
Infrastructure/Infrastructure.csproj
Domain/Domain.csproj
Tests/Tests.csproj
BiblioBot.sln
.gitignore
README.md si existe

No modifiques ningun .csproj.

No instales paquetes.

No modifiques connection strings.

No crees migraciones.

No ejecutes database update.

No ejecutes seeders.

No uses HasData.
