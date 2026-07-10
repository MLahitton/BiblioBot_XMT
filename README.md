# BiblioBot

![Estado](https://img.shields.io/badge/estado-local%20%2F%20demo-2ea44f?style=for-the-badge)
![Monorepo](https://img.shields.io/badge/arquitectura-monorepo-0f172a?style=for-the-badge)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-API-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Next.js 16](https://img.shields.io/badge/Next.js-16-000000?style=for-the-badge&logo=nextdotjs&logoColor=white)
![React 19](https://img.shields.io/badge/React-19-61DAFB?style=for-the-badge&logo=react&logoColor=0f172a)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![FastAPI](https://img.shields.io/badge/FastAPI-chatbot-009688?style=for-the-badge&logo=fastapi&logoColor=white)
![Python](https://img.shields.io/badge/Python-3.x-3776AB?style=for-the-badge&logo=python&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-database-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![JWT](https://img.shields.io/badge/Auth-JWT-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![Swagger](https://img.shields.io/badge/API-Swagger%20%2F%20OpenAPI-85EA2D?style=for-the-badge&logo=swagger&logoColor=0f172a)

BiblioBot es un sistema web tipo ecommerce y biblioteca inteligente para la gestion, venta y consulta de libros. El proyecto integra un backend ASP.NET Core, un frontend Next.js y un chatbot FastAPI capaz de consultar el catalogo real, responder preguntas de disponibilidad y guiar flujos de compra con confirmaciones seguras.

Repositorio: https://github.com/MLahitton/BiblioBot_XMT

## Descripcion general

BiblioBot resuelve la necesidad de administrar un inventario de libros y, al mismo tiempo, ofrecer una experiencia de compra asistida por chatbot.

El sistema permite:

- Explorar catalogo de libros.
- Buscar por titulo, autor, categoria, editorial o texto libre.
- Ver detalle de libros.
- Consultar stock y disponibilidad.
- Agregar libros al carrito.
- Crear ventas pendientes desde el carrito.
- Confirmar ventas.
- Generar facturas desde el flujo transaccional de ventas.
- Gestionar inventario.
- Gestionar solicitudes internas de compra y traslado.
- Gestionar usuarios, roles y permisos.
- Interactuar con el chatbot BiblioBot como invitado o usuario autenticado.

El chatbot permite:

- Buscar libros reales del backend .NET.
- Ver detalle de libros.
- Consultar stock.
- Recomendar libros por categoria o autor.
- Bloquear compras si el usuario es invitado.
- Agregar libros al carrito si el usuario esta autenticado.
- Crear una venta pendiente desde el carrito.
- Confirmar una venta y generar factura si el flujo esta habilitado.
- Entender lenguaje natural como `recomiendame libros de fantasia`, `hablame sobre Matilda, lo tienes?`, `tienes Matilda?`, `quiero comprar 2 El Hobbit`, `finalizar compra` y `confirmar venta`.

## Arquitectura

El proyecto esta organizado como monorepo con tres aplicaciones principales:

| Modulo | Ruta | Responsabilidad |
| --- | --- | --- |
| Backend .NET | `bibliobot_Backend` | API REST, autenticacion, permisos, catalogo, carrito, ventas, facturas, inventario, seeders y persistencia. |
| Chatbot FastAPI | `BiblioBot_Chatbot` | Orquestacion conversacional, deteccion de intenciones, herramientas seguras y cliente HTTP hacia backend .NET. |
| Frontend Next.js | `BiblioBot_Frontend` | Interfaz web para clientes/trabajadores, catalogo, carrito, login y widget de chatbot. |

Flujo general:

```text
Frontend Next.js
  -> Backend .NET
  -> FastAPI Chatbot
  -> Backend .NET real
  -> PostgreSQL
```

Las mutaciones reales sensibles se ejecutan desde .NET. FastAPI conserva `ALLOW_REAL_BACKEND_MUTATIONS=false` para no escribir directamente contra el backend.

## Tecnologias

### Backend

- .NET 10
- ASP.NET Core
- Clean Architecture
- Vertical Slices
- MediatR
- Entity Framework Core
- PostgreSQL
- JWT
- Swagger/OpenAPI

### Chatbot

- Python
- FastAPI
- LangGraph
- LangChain
- Gemini opcional
- Pydantic
- Pytest
- Cliente HTTP hacia backend .NET

### Frontend

- Next.js 16
- React 19
- TypeScript
- Tailwind CSS 4
- Framer Motion
- pnpm
- JWT
- LocalStorage para persistencia visual del chatbot

### Base de datos

- PostgreSQL
- Migraciones EF Core
- Seeders idempotentes para catalogo, inventario, usuarios, roles y permisos

## Estructura del proyecto

```text
BiblioBot/
  bibliobot_Backend/
    Api/
    Application/
    Domain/
    Infrastructure/
    Tests/
  BiblioBot_Chatbot/
    app/
    tests/
    requirements.txt
  BiblioBot_Frontend/
    app/
    config/
    features/
    package.json
  README.md
```

## Requisitos previos

Instala o verifica:

- Git
- .NET SDK 10
- Node.js compatible con Next.js 16
- pnpm
- Python compatible con el proyecto
- PostgreSQL
- DBeaver o pgAdmin opcional
- Visual Studio Code opcional

Comandos de verificacion:

```powershell
git --version
dotnet --version
node --version
pnpm --version
python --version
psql --version
```

Si PowerShell bloquea `pnpm`, usa `pnpm.cmd`:

```powershell
pnpm.cmd --version
```

## Instalacion rapida

Desde la raiz del repositorio:

```powershell
cd C:\Users\mlahi\Desktop\BiblioBot
```

Orden recomendado:

1. Levantar PostgreSQL.
2. Configurar variables de conexion.
3. Restaurar y compilar backend.
4. Aplicar migraciones.
5. Ejecutar seeders.
6. Levantar backend .NET.
7. Instalar y levantar chatbot FastAPI.
8. Instalar y levantar frontend.
9. Probar login y chatbot.

## Configuracion de PostgreSQL

Datos locales recomendados:

| Campo | Valor |
| --- | --- |
| Host | `localhost` |
| Puerto | `5432` |
| Database | `bibliobot` |
| Usuario | `postgres` |
| Password | `1234` |

SQL opcional para DBeaver/pgAdmin:

```sql
ALTER USER postgres WITH PASSWORD '1234';
CREATE DATABASE bibliobot;
```

Si usas otra contrasena, ajusta las variables de entorno del backend.

### Variables de conexion

Para runtime del backend:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=bibliobot;Username=postgres;Password=1234"
```

Para comandos EF Core design-time:

```powershell
$env:BIBLIOBOT_CONNECTION_STRING="Host=localhost;Port=5432;Database=bibliobot;Username=postgres;Password=1234"
```

Por que existen ambas:

| Variable | Uso |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Usada por la API en runtime. |
| `BIBLIOBOT_CONNECTION_STRING` | Usada por `BiblioBotDbContextFactory` para comandos `dotnet ef`. |

## Backend .NET

Entrar al backend:

```powershell
cd bibliobot_Backend
```

Restaurar paquetes:

```powershell
dotnet restore
```

Compilar:

```powershell
dotnet build
```

Listar migraciones:

```powershell
dotnet ef migrations list --project Infrastructure\Infrastructure.csproj --startup-project Api\Api.csproj
```

Aplicar migraciones:

```powershell
dotnet ef database update --project Infrastructure\Infrastructure.csproj --startup-project Api\Api.csproj
```

Ejecutar backend:

```powershell
dotnet run --project Api\Api.csproj --launch-profile http
```

URL esperada:

```text
http://localhost:5218
```

Swagger:

```text
http://localhost:5218/swagger
```

## Seeders y datos reales

Ejecutar seeders:

```powershell
dotnet run --project Api\Api.csproj --launch-profile http -- --seed
```

Si ya compilaste y quieres evitar recompilar:

```powershell
dotnet run --project Api\Api.csproj --launch-profile http --no-build -- --seed
```

El seeder inserta datos de desarrollo/demo:

- Roles.
- Permisos.
- Usuario administrador por defecto.
- Libros reales.
- Autores reales.
- Categorias reales.
- Editoriales reales.
- Sedes.
- Stock inicial.
- Catalogos necesarios para ventas, inventario y solicitudes.

Los seeders son idempotentes: no deben duplicar libros, autores, categorias, editoriales ni stock por libro/sede.

Validaciones despues del seed:

```text
GET http://localhost:5218/api/libros
GET http://localhost:5218/api/libros/search?q=fantasia
GET http://localhost:5218/api/busquedas/autores?q=garcia
GET http://localhost:5218/api/busquedas/categorias?q=programacion
```

## Cuenta administrador por defecto

Cuenta de desarrollo/demo:

| Campo | Valor |
| --- | --- |
| Correo | `admin.bootstrap@bibliobot.test` |
| Contrasena | `Admin_Bootstrap_123!` |

No uses esta contrasena en produccion. Cambia las credenciales en cualquier despliegue real.

El administrador puede, segun permisos:

- Iniciar sesion.
- Gestionar usuarios.
- Gestionar roles.
- Gestionar permisos.
- Crear, editar, activar y desactivar libros.
- Gestionar autores, categorias y editoriales.
- Consultar inventario.
- Registrar movimientos de inventario.
- Consultar ventas.
- Confirmar ventas.
- Consultar facturas.
- Gestionar solicitudes internas.
- Probar el chatbot autenticado.

Login por Swagger:

```http
POST /api/auth/login
```

Body:

```json
{
  "email": "admin.bootstrap@bibliobot.test",
  "password": "Admin_Bootstrap_123!"
}
```

## Chatbot FastAPI

Entrar al chatbot:

```powershell
cd BiblioBot_Chatbot
```

Crear entorno virtual:

```powershell
py -m venv .venv
```

Activar entorno virtual:

```powershell
.\.venv\Scripts\Activate.ps1
```

Actualizar pip:

```powershell
python -m pip install --upgrade pip
```

Instalar dependencias:

```powershell
pip install -r requirements.txt
```

Ejecutar tests:

```powershell
python -m pytest
```

Ejecutar chatbot:

```powershell
uvicorn app.main:app --reload --port 8000
```

URLs:

```text
http://localhost:8000
http://localhost:8000/docs
http://localhost:8000/health
```

## Variables de entorno del chatbot

Crea un archivo `.env` en `BiblioBot_Chatbot` tomando como guia `.env.example`.

Ejemplo:

```env
APP_NAME="BiblioBot Chatbot"
ENVIRONMENT=development

GEMINI_API_KEY=
GEMINI_MODEL=gemini-2.5-flash

DOTNET_API_BASE_URL=http://localhost:5218
DOTNET_API_TIMEOUT_SECONDS=10

USE_MOCK_DOTNET_CLIENT=false
ALLOW_REAL_BACKEND_MUTATIONS=false

CHATBOT_INTERNAL_API_KEY=dev_internal_key
DOTNET_API_BEARER_TOKEN=
```

Notas:

- `USE_MOCK_DOTNET_CLIENT=true` usa datos simulados.
- `USE_MOCK_DOTNET_CLIENT=false` consulta el backend .NET real.
- `ALLOW_REAL_BACKEND_MUTATIONS=false` mantiene FastAPI sin mutaciones directas.
- Las mutaciones reales deben ejecutarse desde .NET, no desde FastAPI.
- `GEMINI_API_KEY` es opcional. Sin clave, el chatbot sigue funcionando con reglas deterministicas.
- No guardes tokens reales en `.env`.

## Mutaciones controladas desde chatbot

Estas variables se configuran en el backend .NET para habilitar flujos reales controlados:

```powershell
$env:BIBLIOBOT_ALLOW_REAL_CART_MUTATIONS="true"
$env:BIBLIOBOT_ALLOW_REAL_SALE_MUTATIONS="true"
$env:BIBLIOBOT_ALLOW_REAL_SALE_CONFIRMATION_MUTATIONS="true"
$env:BIBLIOBOT_ALLOW_REAL_INVENTORY_MUTATIONS="false"
$env:BIBLIOBOT_ALLOW_REAL_INVOICE_MUTATIONS="false"
$env:BIBLIOBOT_ALLOW_REAL_REQUEST_MUTATIONS="false"
```

Significado:

| Variable | Funcion |
| --- | --- |
| `BIBLIOBOT_ALLOW_REAL_CART_MUTATIONS` | Permite agregar productos al carrito desde chatbot. |
| `BIBLIOBOT_ALLOW_REAL_SALE_MUTATIONS` | Permite crear venta pendiente desde carrito. |
| `BIBLIOBOT_ALLOW_REAL_SALE_CONFIRMATION_MUTATIONS` | Permite confirmar venta pendiente. |
| `BIBLIOBOT_ALLOW_REAL_INVENTORY_MUTATIONS` | Reservada para mutaciones directas de inventario. Mantener apagada salvo pruebas controladas. |
| `BIBLIOBOT_ALLOW_REAL_INVOICE_MUTATIONS` | Reservada para mutaciones directas de facturas. Mantener apagada. |
| `BIBLIOBOT_ALLOW_REAL_REQUEST_MUTATIONS` | Reservada para solicitudes internas. Mantener apagada salvo pruebas controladas. |

Importante: confirmar una venta con `ConfirmSaleCommand` puede descontar stock y generar factura como parte del flujo transaccional existente de ventas, aunque las banderas directas de inventario/factura esten apagadas.

FastAPI debe mantenerse con:

```env
ALLOW_REAL_BACKEND_MUTATIONS=false
```

## Frontend Next.js

Entrar al frontend:

```powershell
cd BiblioBot_Frontend
```

Instalar dependencias:

```powershell
pnpm install
```

Si PowerShell bloquea scripts:

```powershell
pnpm.cmd install
```

Ejecutar en desarrollo:

```powershell
pnpm run dev
```

O con `pnpm.cmd`:

```powershell
pnpm.cmd run dev
```

Build:

```powershell
pnpm run build
```

Lint:

```powershell
pnpm run lint
```

URL esperada:

```text
http://localhost:3000
```

## Variables de entorno del frontend

El frontend usa Next.js y variables `NEXT_PUBLIC_*`.

Ejemplo `.env.local` en `BiblioBot_Frontend`:

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5218
NEXT_PUBLIC_API_BROWSER_BASE_URL=/backend-api
```

Notas:

- `NEXT_PUBLIC_API_BASE_URL` apunta al backend .NET.
- `NEXT_PUBLIC_API_BROWSER_BASE_URL` usa por defecto `/backend-api` para el navegador.
- Si cambias el puerto del backend, actualiza estas variables.

## Como ejecutar todo

Desde tres terminales distintas:

### Terminal 1: backend

```powershell
cd C:\Users\mlahi\Desktop\BiblioBot\bibliobot_Backend
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=bibliobot;Username=postgres;Password=1234"
$env:BIBLIOBOT_ALLOW_REAL_CART_MUTATIONS="true"
$env:BIBLIOBOT_ALLOW_REAL_SALE_MUTATIONS="true"
$env:BIBLIOBOT_ALLOW_REAL_SALE_CONFIRMATION_MUTATIONS="true"
dotnet run --project Api\Api.csproj --launch-profile http
```

### Terminal 2: chatbot

```powershell
cd C:\Users\mlahi\Desktop\BiblioBot\BiblioBot_Chatbot
.\.venv\Scripts\Activate.ps1
uvicorn app.main:app --reload --port 8000
```

### Terminal 3: frontend

```powershell
cd C:\Users\mlahi\Desktop\BiblioBot\BiblioBot_Frontend
pnpm.cmd run dev
```

Despues abre:

```text
http://localhost:3000
```

## Pruebas recomendadas del chatbot

### Como invitado

Mensajes:

```text
recomiendame libros de fantasia
hablame sobre matilda, lo tienes?
quiero saber acerca de matilda
tienes matilda?
quiero comprar matilda
```

Esperado:

- Puede buscar catalogo.
- Puede ver detalle.
- Puede consultar stock.
- No puede comprar.
- Debe recibir respuesta `AUTH_REQUIRED` o enlaces para iniciar sesion/crear cuenta cuando intente comprar.

### Como usuario autenticado

Mensajes:

```text
quiero comprar 2 El Hobbit
si confirmo
finalizar compra
si confirmo
confirmar venta
si confirmo
```

Esperado:

- Primero agrega el libro al carrito.
- Luego crea una venta pendiente desde carrito.
- Luego confirma la venta.
- Al confirmar, descuenta stock y genera factura si la configuracion lo permite.
- Si la venta no tiene sede, el sistema pedira sede antes de confirmar.

## Flujo E2E recomendado

1. Iniciar sesion como administrador o usuario con permisos.
2. Buscar libros de fantasia desde el chatbot.
3. Ver detalle de `El Hobbit`.
4. Consultar stock de `El Hobbit`.
5. Comprar 2 unidades.
6. Confirmar la accion del carrito.
7. Abrir carrito.
8. Finalizar compra.
9. Confirmar creacion de venta pendiente.
10. Confirmar venta.
11. Ver factura generada.

Algunos pasos requieren estas banderas activas en backend:

```powershell
$env:BIBLIOBOT_ALLOW_REAL_CART_MUTATIONS="true"
$env:BIBLIOBOT_ALLOW_REAL_SALE_MUTATIONS="true"
$env:BIBLIOBOT_ALLOW_REAL_SALE_CONFIRMATION_MUTATIONS="true"
```

## Endpoints principales

### Auth

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
GET  /api/auth/me
```

### Chat

```text
POST /api/chat/public-message
POST /api/chat/message
```

### Libros

```text
GET    /api/libros
GET    /api/libros/search
GET    /api/libros/{id}
POST   /api/libros
PUT    /api/libros/{id}
PATCH  /api/libros/{id}/activar
PATCH  /api/libros/{id}/desactivar
GET    /api/libros/favoritos
GET    /api/libros/{bookId}/favorito
POST   /api/libros/{bookId}/favorito
DELETE /api/libros/{bookId}/favorito
```

### Carrito

```text
POST   /api/carrito
GET    /api/carrito/{sessionId}
DELETE /api/carrito/{sessionId}/items/{bookId}
DELETE /api/carrito/{sessionId}
```

### Ventas

```text
POST /api/ventas
POST /api/ventas/{id}/confirmar
GET  /api/ventas
GET  /api/ventas/{id}
```

### Facturas

```text
GET /api/facturas
GET /api/facturas/{id}
GET /api/facturas/venta/{saleId}
```

### Inventario

```text
GET  /api/inventario
GET  /api/inventario/movimientos
POST /api/inventario/entradas
POST /api/inventario/salidas
POST /api/inventario/ajustes
```

### Lookups

```text
GET /api/busquedas/libros
GET /api/busquedas/autores
GET /api/busquedas/categorias
GET /api/busquedas/editoriales
GET /api/busquedas/sedes
GET /api/busquedas/usuarios
GET /api/busquedas/roles
GET /api/busquedas/ventas
GET /api/busquedas/facturas
GET /api/busquedas/solicitudes
```

### Admin

```text
GET    /api/admin/usuarios
GET    /api/admin/usuarios/{id}
POST   /api/admin/usuarios
PUT    /api/admin/usuarios/{id}
DELETE /api/admin/usuarios/{id}
PATCH  /api/admin/usuarios/{id}/activar
PATCH  /api/admin/usuarios/{id}/desactivar
GET    /api/admin/roles
GET    /api/admin/permisos
POST   /api/admin/usuarios/{id}/roles
DELETE /api/admin/usuarios/{id}/roles/{roleCode}
```

### Solicitudes internas

```text
POST  /api/solicitudes/compras
POST  /api/solicitudes/traslados
GET   /api/solicitudes
GET   /api/solicitudes/{id}
PATCH /api/solicitudes/{id}/aprobar
PATCH /api/solicitudes/{id}/rechazar
PATCH /api/solicitudes/{id}/ejecutar
```

### Reportes

```text
GET /api/reportes/ventas
GET /api/reportes/inventario
GET /api/reportes/libros-mas-vendidos
GET /api/reportes/stock-bajo
```

## Comandos de build y test

Backend:

```powershell
cd bibliobot_Backend
dotnet restore
dotnet build
```

Chatbot:

```powershell
cd BiblioBot_Chatbot
.\.venv\Scripts\Activate.ps1
python -m pytest
```

Frontend:

```powershell
cd BiblioBot_Frontend
pnpm.cmd run lint
pnpm.cmd run build
```

## Troubleshooting

### `pytest` no se reconoce

Activa el entorno virtual y usa:

```powershell
python -m pytest
```

### No existe `requirements.txt`

Verifica que estes dentro de:

```powershell
cd BiblioBot_Chatbot
```

### PostgreSQL error `28P01 password authentication failed`

Revisa usuario/contrasena y configura:

```powershell
$env:BIBLIOBOT_CONNECTION_STRING="Host=localhost;Port=5432;Database=bibliobot;Username=postgres;Password=1234"
$env:ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=bibliobot;Username=postgres;Password=1234"
```

### `dotnet ef` usa otra conexion

`dotnet ef` puede usar `BiblioBotDbContextFactory`, que lee `BIBLIOBOT_CONNECTION_STRING`.

Configura antes de ejecutar migraciones:

```powershell
$env:BIBLIOBOT_CONNECTION_STRING="Host=localhost;Port=5432;Database=bibliobot;Username=postgres;Password=1234"
```

### Puerto 5218 ocupado

```powershell
netstat -ano | findstr :5218
taskkill /PID <PID> /F
```

### Puerto 8000 ocupado

```powershell
netstat -ano | findstr :8000
taskkill /PID <PID> /F
```

### PowerShell bloquea `pnpm`

Usa `pnpm.cmd`:

```powershell
pnpm.cmd install
pnpm.cmd run dev
```

### `pnpm install` dice que no existe `package.json`

Entra al frontend:

```powershell
cd BiblioBot_Frontend
pnpm.cmd install
```

### Chatbot devuelve datos mock

Revisa `.env` del chatbot:

```env
USE_MOCK_DOTNET_CLIENT=false
DOTNET_API_BASE_URL=http://localhost:5218
```

### Chatbot no agrega al carrito

Verifica:

- Usuario autenticado.
- Permiso `cart.manage`.
- Backend levantado.
- Flag activo:

```powershell
$env:BIBLIOBOT_ALLOW_REAL_CART_MUTATIONS="true"
```

### Venta no se crea desde chatbot

Verifica:

- Usuario autenticado.
- Permiso `sales.create`.
- Carrito con items.
- Flag activo:

```powershell
$env:BIBLIOBOT_ALLOW_REAL_SALE_MUTATIONS="true"
```

### Venta no se confirma

Verifica:

- Usuario autenticado.
- Permiso `sales.confirm`.
- Stock suficiente.
- Venta con sede/branchId.
- Flag activo:

```powershell
$env:BIBLIOBOT_ALLOW_REAL_SALE_CONFIRMATION_MUTATIONS="true"
```

## Seguridad y notas de produccion

- Este README documenta un entorno local/demo.
- No uses credenciales demo en produccion.
- No guardes tokens JWT reales en archivos `.env`.
- Cambia secretos JWT antes de despliegues reales.
- Mantener `ALLOW_REAL_BACKEND_MUTATIONS=false` en FastAPI salvo pruebas controladas.
- Las mutaciones sensibles deben pasar por .NET, permisos y JWT.
- Revisa CORS, HTTPS, logs y politicas de contrasenas antes de produccion.

## Estado actual del proyecto

Estado funcional local:

- Backend .NET con Swagger.
- PostgreSQL con migraciones y seeders.
- Frontend Next.js con chatbot integrado.
- Chatbot FastAPI con deteccion deterministica de intenciones.
- Flujo invitado: catalogo, detalle y stock permitidos; compra bloqueada.
- Flujo autenticado: carrito real, venta pendiente y confirmacion de venta con factura mediante flags controlados.

El proyecto esta preparado para pruebas academicas/locales y puede evolucionar hacia despliegue real reforzando seguridad, configuracion de secretos, observabilidad y despliegue.
