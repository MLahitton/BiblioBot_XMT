# BiblioBot - Reporte de Handoff Técnico (Dossier General)

## 1) Objetivo y estado actual del proyecto

Proyecto: **BiblioBot**  
Stack principal: **.NET 10**, **ASP.NET Core API**, **Entity Framework Core**, arquitectura **Clean Architecture (4 capas)**, con autenticación JWT, permisos por claims y seed de datos.

Estado actual:
- API funcional con controladores y flujo de negocio implementado para:
  - Autenticación/Autorización
  - Catálogos (`libros`, `autores`, `categorías`, `editoriales`)
  - Carrito + ventas
  - Inventario y movimientos
  - Solicitudes internas (compra/traslado)
  - Chat
  - Admin (usuarios, roles y permisos)
  - Reportes
- Se corrigió el mapeo de permisos para que el rol **ADMIN** incluya `cart.manage` y mantenga capacidad total de carrito/ventas.
- Se detectaron y resolvieron errores de Swagger por falta de paquete (`Swashbuckle.AspNetCore`) y usings, al agregar la referencia y el pipeline correspondiente.
- Base de datos y JWT se configuran por `appsettings` y `appsettings.Development`.

---

## 2) Estructura del repositorio

- Repositorio raíz: `C:\Users\mlahi\Desktop\BiblioBot`
- Proyectos:
  - [`bibliobot_Backend/Api/Api.csproj`](bibliobot_Backend/Api/Api.csproj)
  - [`bibliobot_Backend/Application/Application.csproj`](bibliobot_Backend/Application/Application.csproj)
  - [`bibliobot_Backend/Domain/Domain.csproj`](bibliobot_Backend/Domain/Domain.csproj)
  - [`bibliobot_Backend/Infrastructure/Infrastructure.csproj`](bibliobot_Backend/Infrastructure/Infrastructure.csproj)
  - [`bibliobot_Backend/Tests/Tests.csproj`](bibliobot_Backend/Tests/Tests.csproj)
- Documentos de contexto existentes (además de este reporte):
  - [`Bibliobot.md`](Bibliobot.md)
  - `.agents/BiblioBot_Combinado_Negocio_y_Tecnico.md`
  - `.agents/Tomas.md`
  - `.agents/Manuel.md`
  - `.agents/Ximena.md`
  - `.agents/.` otros prompts de instrucciones previas

---

## 3) Capa API (presentación)

Namespace raíz: `Api`

Archivos principales:
- [`bibliobot_Backend/Api/Program.cs`](bibliobot_Backend/Api/Program.cs)
- [`bibliobot_Backend/Api/Extensions/AuthorizationExtensions.cs`](bibliobot_Backend/Api/Extensions/AuthorizationExtensions.cs)

### Dependency/Bootstrapping principal
- `Program.cs`:
  - `builder.Services.AddApplicationServices();`
  - `builder.Services.AddInfrastructureServices(builder.Configuration);`
  - JWT Bearer con `JwtOptions`
  - `builder.Services.AddPermissionAuthorization();`
  - Swagger:
    - `AddSwaggerGen()` y esquema `Bearer`
    - `UseSwagger()` + `UseSwaggerUI()` en Development
  - `UseAuthentication()`, `UseAuthorization()`, `MapControllers()`
  - soporte de seed por argumento `--seed`
    - `dotnet run --project Api --seed`
- Ruta Swagger: `/swagger`
- OpenAPI JSON path: `/openapi/v1.json`

### Endpoints (controladores y permisos)

Archivo base para cada grupo:
- [`bibliobot_Backend/Api/Controllers/AdminController.cs`](bibliobot_Backend/Api/Controllers/AdminController.cs)
- [`bibliobot_Backend/Api/Controllers/AuthController.cs`](bibliobot_Backend/Api/Controllers/AuthController.cs)
- [`bibliobot_Backend/Api/Controllers/AuthorsController.cs`](bibliobot_Backend/Api/Controllers/AuthorsController.cs)
- [`bibliobot_Backend/Api/Controllers/BooksController.cs`](bibliobot_Backend/Api/Controllers/BooksController.cs)
- [`bibliobot_Backend/Api/Controllers/BranchesController.cs`](bibliobot_Backend/Api/Controllers/BranchesController.cs)
- [`bibliobot_Backend/Api/Controllers/CartController.cs`](bibliobot_Backend/Api/Controllers/CartController.cs)
- [`bibliobot_Backend/Api/Controllers/CategoriesController.cs`](bibliobot_Backend/Api/Controllers/CategoriesController.cs)
- [`bibliobot_Backend/Api/Controllers/ChatController.cs`](bibliobot_Backend/Api/Controllers/ChatController.cs)
- [`bibliobot_Backend/Api/Controllers/InternalRequestsController.cs`](bibliobot_Backend/Api/Controllers/InternalRequestsController.cs)
- [`bibliobot_Backend/Api/Controllers/InventoryController.cs`](bibliobot_Backend/Api/Controllers/InventoryController.cs)
- [`bibliobot_Backend/Api/Controllers/InvoicesController.cs`](bibliobot_Backend/Api/Controllers/InvoicesController.cs)
- [`bibliobot_Backend/Api/Controllers/PublishersController.cs`](bibliobot_Backend/Api/Controllers/PublishersController.cs)
- [`bibliobot_Backend/Api/Controllers/ReportsController.cs`](bibliobot_Backend/Api/Controllers/ReportsController.cs)
- [`bibliobot_Backend/Api/Controllers/SalesController.cs`](bibliobot_Backend/Api/Controllers/SalesController.cs)

#### Rutas rápidas (resumen)

| Base | Método | Ruta | Auth/Policy |
|---|---|---|---|
| `/api/auth` | `POST` | `/register` | público |
| `/api/auth` | `POST` | `/login` | público |
| `/api/auth` | `POST` | `/refresh` | público |
| `/api/auth` | `GET` | `/me` | `Authorize` |

| `/api/libros` | `POST` | `/` | `books.create` |
| `/api/libros` | `PUT` | `/{id:guid}` | `books.update` |
| `/api/libros` | `PATCH` | `/{id:guid}/desactivar` | `books.disable` |
| `/api/libros` | `PATCH` | `/{id:guid}/activar` | `books.activate` |
| `/api/libros` | `GET` | `/` | público |
| `/api/libros` | `GET` | `/{id:guid}` | público |
| `/api/libros` | `GET` | `/search` | público |

| `/api/autores` | `POST` | `/` | `books.create` |
| `/api/autores` | `PUT` | `/{id:guid}` | `books.update` |
| `/api/autores` | `PATCH` | `/{id:guid}/desactivar` | `books.update` |
| `/api/autores` | `PATCH` | `/{id:guid}/activar` | `books.update` |
| `/api/autores` | `GET` | `/` y `/{id:guid}` | público |

| `/api/categorias` | `POST` | `/` | `books.create` |
| `/api/categorias` | `PUT` | `/{id:guid}` | `books.update` |
| `/api/categorias` | `PATCH` | `/{id:guid}/desactivar` | `books.update` |
| `/api/categorias` | `PATCH` | `/{id:guid}/activar` | `books.update` |
| `/api/categorias` | `GET` | `/` y `/{id:guid}` | público |

| `/api/editoriales` | `POST` | `/` | `books.create` |
| `/api/editoriales` | `PUT` | `/{id:guid}` | `books.update` |
| `/api/editoriales` | `PATCH` | `/{id:guid}/desactivar` | `books.update` |
| `/api/editoriales` | `PATCH` | `/{id:guid}/activar` | `books.update` |
| `/api/editoriales` | `GET` | `/` y `/{id:guid}` | público |

| `/api/carrito` | `POST` | `/` | `cart.manage` |
| `/api/carrito` | `GET` | `/{sessionId}` | `cart.read` |
| `/api/carrito` | `DELETE` | `/{sessionId}` | `cart.manage` |
| `/api/carrito` | `DELETE` | `/{sessionId}/items/{bookId:guid}` | `cart.manage` |

| `/api/ventas` | `POST` | `/` | `sales.create` |
| `/api/ventas` | `POST` | `/{id:guid}/confirmar` | `sales.confirm` |
| `/api/ventas` | `GET` | `/` | `Authorize` + `sales.read_all` o `sales.read_own` |
| `/api/ventas` | `GET` | `/{id:guid}` | `Authorize` + `sales.read_all` o `sales.read_own` |

| `/api/facturas` | `GET` | `/` | `Authorize` |
| `/api/facturas` | `GET` | `/{id:guid}` | `Authorize` |
| `/api/facturas` | `GET` | `/venta/{saleId:guid}` | `Authorize` |

| `/api/sedes` | `GET` | `/` | `inventory.read` |
| `/api/sedes` | `GET` | `/{id:guid}` | `inventory.read` |
| `/api/sedes` | `POST` | `/` | `inventory.adjust` |
| `/api/sedes` | `PUT` | `/{id:guid}` | `inventory.adjust` |
| `/api/sedes` | `PATCH` | `/{id:guid}/activar` | `inventory.adjust` |
| `/api/sedes` | `PATCH` | `/{id:guid}/desactivar` | `inventory.adjust` |

| `/api/inventario` | `GET` | `/` | `inventory.read` |
| `/api/inventario` | `GET` | `/movimientos` | `inventory.read` |
| `/api/inventario` | `POST` | `/entradas` | `inventory.entry` |
| `/api/inventario` | `POST` | `/salidas` | `inventory.exit` |
| `/api/inventario` | `POST` | `/ajustes` | `inventory.adjust` |

| `/api/solicitudes` | `POST` | `/compras` | `requests.purchase.create` |
| `/api/solicitudes` | `POST` | `/traslados` | `requests.transfer.create` |
| `/api/solicitudes` | `GET` | `/` | `Authorize` |
| `/api/solicitudes` | `GET` | `/{id:guid}` | `Authorize` |
| `/api/solicitudes` | `PATCH` | `/{id:guid}/aprobar` | `requests.approve` |
| `/api/solicitudes` | `PATCH` | `/{id:guid}/rechazar` | `requests.reject` |
| `/api/solicitudes` | `PATCH` | `/{id:guid}/ejecutar` | `requests.execute` |

| `/api/chat` | `POST` | `/message` | `Authorize` |

| `/api/reportes` | `GET` | `/ventas` | `reports.sales.read` |
| `/api/reportes` | `GET` | `/inventario` | `reports.inventory.read` |
| `/api/reportes` | `GET` | `/libros-mas-vendidos` | `reports.sales.read` |
| `/api/reportes` | `GET` | `/stock-bajo` | `reports.inventory.read` |

| `/api/admin` | `GET` | `/usuarios` | `admin.users.read` |
| `/api/admin` | `GET` | `/usuarios/{id:guid}` | `admin.users.read` |
| `/api/admin` | `POST` | `/usuarios` | `Authorize` |
| `/api/admin` | `PATCH` | `/usuarios/{id:guid}/activar` | `admin.users.read` |
| `/api/admin` | `PATCH` | `/usuarios/{id:guid}/desactivar` | `admin.users.read` |
| `/api/admin` | `POST` | `/usuarios/{id:guid}/roles` | `admin.users.read` + `admin.roles.read` |
| `/api/admin` | `DELETE` | `/usuarios/{id:guid}/roles/{roleCode}` | `admin.users.read` + `admin.roles.read` |
| `/api/admin` | `GET` | `/roles` | `admin.roles.read` |
| `/api/admin` | `GET` | `/permisos` | `admin.permissions.read` |

### Contratos request (Api.Contracts)

Ubicación base: [`bibliobot_Backend/Api/Contracts`](bibliobot_Backend/Api/Contracts)

- Auth: `RegisterRequest`, `LoginRequest`, `RefreshTokenRequest`
- Cart: `AddOrUpdateCartItemRequest` (`SessionId`, `BookId`, `Quantity`, `BranchId?`)
- Sales: `CreateSaleRequest` (`SessionId`, `BranchId?`, `OriginCode?`)
- Admin: `CreateAdminUserRequest`, `AssignUserRoleRequest`
- Branches: `CreateBranchRequest`, `UpdateBranchRequest`
- Inventory: `RegisterInventoryEntryRequest`, `RegisterInventoryExitRequest`, `RegisterInventoryAdjustmentRequest`
- InternalRequests: `CreatePurchaseRequestRequest`, `CreateTransferRequestRequest`, `ApproveInternalRequestRequest`, `RejectInternalRequestRequest`, `ExecuteInternalRequestRequest`, `InternalRequestItemRequest`
- Chat: `SendChatMessageRequest`
- Catálogos: `CreateAuthorRequest`, `UpdateAuthorRequest`, `CreateCategoryRequest`, `UpdateCategoryRequest`, `CreatePublisherRequest`, `UpdatePublisherRequest`
- Books: `CreateBookRequest`, `UpdateBookRequest`

---

## 4) Capa Application

Namespace raíz: `Application`

Responsabilidad:
- Contiene casos de uso (CQRS con MediatR), DTOs y contratos de persistencia.
- Registra handlers en `AddApplicationServices`.
- Usa `IApplicationDbContext` para desacoplar EF.

Archivos de referencia:
- [`bibliobot_Backend/Application/DependencyInjection.cs`](bibliobot_Backend/Application/DependencyInjection.cs)
- [`bibliobot_Backend/Application/Common/Interfaces`](bibliobot_Backend/Application/Common/Interfaces)
- [`bibliobot_Backend/Application/Common/Security/PermissionAuthorizationHandler.cs`](bibliobot_Backend/Application/Common/Security/PermissionAuthorizationHandler.cs)
- [`bibliobot_Backend/Application/Common/Security/PermissionRequirement.cs`](bibliobot_Backend/Application/Common/Security/PermissionRequirement.cs)

Seguridad de permisos:
- Handler (`IAuthorizationRequirement`) valida claim `permission` contra `PermissionRequirement.Permission`.
- Se definen políticas por código de permiso en `Api/Extensions/AuthorizationExtensions.cs`.

Features por carpeta (`Application/Features`):
- `Auth`
- `Admin`
- `Books`
- `Cart`
- `Inventory`
- `Sales`
- `Invoices`
- `InternalRequests`
- `Branches`
- `Reports`
- `Chat`
- `Catalog`

---

## 5) Capa Domain

Namespace raíz: `Domain`

Qué contiene:
- Entidades del dominio en [`bibliobot_Backend/Domain/Entities`](bibliobot_Backend/Domain/Entities)
- Constantes/identificadores funcionales en [`bibliobot_Backend/Domain/Constants`](bibliobot_Backend/Domain/Constants)
- `BaseEntity` con `Id` base (`Guid`)

Entidades listadas:
- `User`, `Role`, `Permission`, `UserRole`, `RolePermission`, `RefreshToken`
- `Book`, `Author`, `Publisher`, `Category`, `BookAuthor`, `BookCategory`
- `Cart`, `CartItem`
- `Sale`, `SaleDetail`, `SaleStatus`, `SaleOrigin`
- `Invoice`
- `Branch`, `InventoryStock`, `InventoryMovement`, `InventoryMovementType`
- `RequestType`, `RequestStatus`, `InternalRequest`, `InternalRequestItem`
- `ChatConversation`, `ChatLog`

Constantes de dominio importantes:
- [`RoleCodes`](bibliobot_Backend/Domain/Constants/RoleCodes.cs): `CLIENT`, `WORKER`, `ADMIN`
- [`PermissionCodes`](bibliobot_Backend/Domain/Constants/PermissionCodes.cs): permisos funcionales completos
- [`SaleStatusCodes`](bibliobot_Backend/Domain/Constants/SaleStatusCodes.cs): `CREATED`, `PENDING_CONFIRMATION`, `CONFIRMED`, `REJECTED`, `CANCELLED`
- [`SaleOriginCodes`](bibliobot_Backend/Domain/Constants/SaleOriginCodes.cs): `WEB_UI`, `CHATBOT`
- [`RequestTypeCodes`](bibliobot_Backend/Domain/Constants/RequestTypeCodes.cs): `PURCHASE`, `TRANSFER`
- [`RequestStatusCodes`](bibliobot_Backend/Domain/Constants/RequestStatusCodes.cs): `CREATED`, `IN_REVIEW`, `APPROVED`, `REJECTED`, `EXECUTED`
- [`InventoryMovementTypeCodes`](bibliobot_Backend/Domain/Constants/InventoryMovementTypeCodes.cs): `ENTRY`, `EXIT`, `ADJUSTMENT`, `SALE`, `TRANSFER_IN`, `TRANSFER_OUT`
- [`CartStatusCodes`](bibliobot_Backend/Domain/Constants/CartStatusCodes.cs): `ACTIVE`, `CHECKED_OUT`, `CANCELLED`, `ABANDONED`

---

## 6) Capa Infrastructure

Namespace raíz: `Infrastructure`

Servicios registrados en [`bibliobot_Backend/Infrastructure/DependencyInjection.cs`](bibliobot_Backend/Infrastructure/DependencyInjection.cs):
- DbContext PostgreSQL (`BiblioBotDbContext`)
- `IApplicationDbContext`
- `IDatabaseSeeder`
- `IJwtTokenGenerator`, `IPasswordHasher`, `IRefreshTokenService`, `ICurrentUserService`
- `IChatbotClient` (HTTP client tipado)

Seguridad/Token:
- [`bibliobot_Backend/Infrastructure/Security/JwtOptions.cs`](bibliobot_Backend/Infrastructure/Security/JwtOptions.cs)
- [`JwtTokenGenerator`](bibliobot_Backend/Infrastructure/Security/JwtTokenGenerator.cs) emite claims `sub`, `email`, `fullName`, `permission`, `role`
- [`PasswordHasher`](bibliobot_Backend/Infrastructure/Security/PasswordHasher.cs) para validación/hash de contraseña
- [`RefreshTokenService`](bibliobot_Backend/Infrastructure/Security/RefreshTokenService.cs)
- [`CurrentUserService`](bibliobot_Backend/Infrastructure/Security/CurrentUserService.cs)
- [`FastApiChatbotClient`](bibliobot_Backend/Infrastructure/Chatbot/FastApiChatbotClient.cs)

Persistencia y seeding:
- [`bibliobot_Backend/Infrastructure/Persistence/BiblioBotDbContext.cs`](bibliobot_Backend/Infrastructure/Persistence/BiblioBotDbContext.cs)
- [`bibliobot_Backend/Infrastructure/Persistence/SeedData/BiblioBotDatabaseSeeder.cs`](bibliobot_Backend/Infrastructure/Persistence/SeedData/BiblioBotDatabaseSeeder.cs)
- semillas: `AuthSeedData`, `CatalogSeedData`, `RolePermissionSeedData` y registros auxiliares.

---

## 7) Configuración de ejecución

Launch profiles:
- [`bibliobot_Backend/Api/Properties/launchSettings.json`](bibliobot_Backend/Api/Properties/launchSettings.json)
- HTTP: `http://localhost:5218`
- HTTPS profile también expone `https://localhost:7155` y `http://localhost:5218`

App settings:
- [`bibliobot_Backend/Api/appsettings.json`](bibliobot_Backend/Api/appsettings.json)
- [`bibliobot_Backend/Api/appsettings.Development.json`](bibliobot_Backend/Api/appsettings.Development.json)
- `Jwt.Secret` actual está hardcodeado con valor de desarrollo, debe reemplazarse en entorno productivo.
- `ConnectionStrings:DefaultConnection` apunta por defecto a PostgreSQL local.
- `Chatbot` apunta por defecto a `http://localhost:8000`.

---

## 8) Seguridad y autorización por permisos

Flujo general:
- Login genera `AccessToken` (JWT) y `RefreshToken`.
- Cada endpoint protegido usa:
  - `Authorize` (autenticación básica), o
  - `Authorize(Policy = "...")` para permisos explícitos.
- Claims de permisos:
  - Se agregan desde base de datos a `IJwtTokenGenerator` al iniciar sesión.
  - El handler valida si claim `permission == codigo`.

Permisos definidos en `PermissionCodes` y usados en policies:
- `auth.me`, `auth.logout`, `auth.change_password`
- `books.read`, `books.search`, `books.create`, `books.update`, `books.disable`, `books.activate`
- `cart.manage`, `cart.read`
- `sales.create`, `sales.confirm`, `sales.read_own`, `sales.read_all`, `sales.cancel`
- `invoices.read_own`, `invoices.read_all`
- `inventory.read`, `inventory.entry`, `inventory.exit`, `inventory.adjust`
- `requests.purchase.create`, `requests.transfer.create`, `requests.read`, `requests.review`, `requests.approve`, `requests.reject`, `requests.execute`
- `admin.users.read`, `admin.roles.read`, `admin.permissions.read`, `admin.permissions.manage`
- `chat.message`, `chat.logs.read`
- `reports.sales.read`, `reports.inventory.read`

Notas de permisos:
- En el seed, los roles `CLIENT`, `WORKER` y `ADMIN` tienen conjuntos base.
- Se corrigió `ADMIN` para que tenga `cart.manage` explícitamente.
- `SeedAsync` vuelve idempotente y además rellena a ADMIN con **todos** los permisos definidos en `AuthSeedData.Permissions` (si faltan).

---

## 9) Flujo recomendado para seguir desarrollando

1) Entender estado actual
- Confirmar ejecución local: perfil `http` (`http://localhost:5218`).
- Revisar logs de API (`temp_api_*.log` si existen).
- Validar que DB Postgres y cadena de conexión funcionan.

2) Preparar seed y seguridad
- Ejecutar seed en desarrollo:
  - `dotnet run --project bibliobot_Backend/Api/Api.csproj --seed`
- Validar usuario bootstrap:
  - Email: `admin.bootstrap@bibliobot.test`
  - Password semilla: `Admin_Bootstrap_123!`
  - Rol asociado: `ADMIN`

3) Probar autenticación y token
- `POST /api/auth/login` -> guardar `accessToken`.
- Probar `GET /api/auth/me`.

4) Probar flujo cliente (carrito + venta)
- `POST /api/carrito` con `cart.manage`.
- `POST /api/ventas` con `sales.create`.
- `POST /api/ventas/{id}/confirmar` con `sales.confirm`.

5) Probar admin (si corresponde)
- Gestionar usuarios/roles/permissions usando `/api/admin/*`.
- Verificar que el token de usuario admin contenga permisos esperados.

6) Reportes y consulta
- `/api/reportes/ventas`, `/api/reportes/inventario`, etc.

---

## 10) Estado funcional clave + fix histórico aplicado

Fix que dejó trazabilidad de bug:
- Se corrigió el permiso de administrador para manejar carrito (necesario para el endpoint `POST /api/carrito`):
  - Archivo: [`bibliobot_Backend/Infrastructure/Persistence/SeedData/RolePermissionSeedData.cs`](bibliobot_Backend/Infrastructure/Persistence/SeedData/RolePermissionSeedData.cs)
  - Cambios: insertar permiso `PermissionCodes.CartManage` dentro del bloque `RoleCodes.Admin`.

Fix de swagger/documentación:
- Error previo reportado:
  - `CS0234`: namespace `Microsoft.OpenApi.Models`/`Models` no encontrado
  - `CS1061`: métodos `AddSwaggerGen`, `UseSwagger`, `UseSwaggerUI` no encontrados
- Estado objetivo:
  - `Swashbuckle.AspNetCore` presente en `Api.csproj`
  - `using Microsoft.OpenApi.Models;` en `Program.cs`
  - `UseSwagger`/`UseSwaggerUI` invocados bajo `if (app.Environment.IsDevelopment())`

---

## 11) Comandos de operación y diagnóstico

- Levantar API (HTTP):
  - `dotnet run --project bibliobot_Backend/Api/Api.csproj --launch-profile http`
- Levantar con restore:
  - `dotnet run --project bibliobot_Backend/Api/Api.csproj --launch-profile http --no-restore` (si ya fue restaurado)
- Seed:
  - `dotnet run --project bibliobot_Backend/Api/Api.csproj --seed`
- Limpiar procesos dotnet:
  - (externo según necesidad del entorno)
- Build:
  - `dotnet restore`
  - `dotnet build`

---

## 12) Estado de pruebas

En el histórico de interacción se ejecutaron pruebas manuales de E2E desde terminal/Swagger:
- Login + `GET /api/auth/me` (válido con token)
- `POST /api/carrito` (creó/actualizó carrito)
- `POST /api/ventas` (creó venta con `PENDING_CONFIRMATION`)
- `POST /api/ventas/{id}/confirmar` devolvió error en ciertos escenarios por estado/permiso del token en esos runs

Pendientes a validar por siguiente instancia:
- Validar nuevamente con token admin activo y `sales.confirm` efectivo.
- Confirmar que `branchId` llega para ventas con stock/flujo que requiera sede.
- Ejecutar corrida limpia de E2E cubriendo: carrito, venta, confirmación, facturas y reportes.

---

## 13) Notas para next agent (muy importante)

- Mantener `seed` idempotente: revisar si agregas nuevos permisos/roles para no romper ID determinísticos.
- Cualquier cambio en permisos debe:
  1) agregarse a `PermissionCodes`
  2) agregarse a `Permission seed` (`AuthSeedData.Permissions`)
  3) agregar policy en `Api/Extensions/AuthorizationExtensions.cs`
  4) mapear en `RolePermissionSeedData` y/o lógica de `Admin` all-permission fallback si aplica.
- Si cambias contrato del front para compra/ventas, respetar rutas de `api/carrito` y `api/ventas` porque son centrales.
- Si hay conflicto de `Unauthorized` en endpoints protegidos, verificar:
  - token no caducado
  - policy requerida en endpoint
  - claim `permission` en JWT realmente emitido.

