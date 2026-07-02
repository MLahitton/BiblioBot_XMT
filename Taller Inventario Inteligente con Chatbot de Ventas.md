# Taller Proyecto: Inventario Inteligente con Chatbot de Ventas

## 1. Nombre del proyecto

**SmartInventory AI**

Sistema inteligente para control de inventario, ventas y facturación asistida por chatbot.

------

## 2. Tecnologías principales

El taller integra las siguientes tecnologías:

- LangChain
- LangGraph
- FastAPI o StreamLIt
- React con TypeScript o Html+Vanilla
- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- pgvector

------

## 3. Descripción general

En este taller se desarrollará un sistema completo de control de inventario en el que los usuarios podrán consultar productos, validar existencias y realizar compras mediante un chatbot inteligente.

El sistema deberá verificar primero si el producto solicitado existe. Si existe, deberá revisar si tiene unidades disponibles. Cuando el usuario confirme la compra, el sistema registrará la venta, actualizará automáticamente el inventario, marcará que la venta fue realizada por el chatbot y generará la factura correspondiente.

El chatbot será desarrollado en Python usando LangChain y LangGraph, pero será expuesto mediante una API construida con FastAPI. Sin embargo, **el frontend en React no consumirá directamente esta API**, sino que todas las solicitudes pasarán por el backend en .NET, el cual actuará como intermediario.

El backend principal del negocio estará construido con ASP.NET Core y Entity Framework Core. Este backend será el encargado de gestionar productos, inventario, ventas, facturas, la conexión con PostgreSQL y también de orquestar la comunicación con el chatbot.

------

## 4. Objetivo general

Desarrollar una aplicación fullstack con inteligencia artificial capaz de gestionar productos, controlar inventario, atender solicitudes mediante chatbot, registrar ventas, actualizar existencias y generar facturas de manera automática.

------

## 5. Objetivos específicos

Al finalizar el taller, el estudiante deberá ser capaz de:

- Diseñar una solución fullstack que combine frontend, backend, base de datos y servicios de inteligencia artificial.
- Crear una API principal con ASP.NET Core y Entity Framework Core.
- Crear una API de chatbot con FastAPI.
- Modelar una base de datos en PostgreSQL para productos, inventario, ventas, clientes y facturas.
- Usar pgvector para permitir búsquedas inteligentes o semánticas de productos.
- Crear un chatbot con LangChain.
- Controlar el flujo de conversación usando LangGraph.
- Conectar el chatbot con la API de .NET mediante peticiones HTTP.
- Conectar React con el backend .NET para consumir el chatbot.
- Validar la existencia y disponibilidad de productos antes de vender.
- Actualizar automáticamente el inventario después de una compra.
- Registrar si una venta fue realizada desde el chatbot.
- Generar una factura asociada a la venta.
- Construir una interfaz administrativa completa con React y TypeScript.

------

## 6. Contexto del problema

Una tienda necesita mejorar su proceso de ventas e inventario. Actualmente, cuando un cliente pregunta por un producto, una persona debe revisar manualmente si el producto existe, confirmar si hay stock disponible, registrar la venta, descontar el inventario y generar la factura.

Este proceso puede ser lento, poco escalable y propenso a errores.

La solución propuesta consiste en crear un sistema donde un chatbot pueda asistir al cliente durante el proceso de compra. El chatbot deberá consultar información real del sistema, validar existencias y crear la venta únicamente cuando el usuario confirme la operación.

------

## 7. Arquitectura general del sistema

El sistema estará dividido en cuatro componentes principales:

1. **Frontend en React con TypeScript**
   Será la interfaz visual del sistema. Permitirá administrar productos, inventario, ventas, facturas y usar el chatbot.
2. **API de negocio en ASP.NET Core**
   Será el backend principal. Contendrá la lógica de productos, inventario, ventas, facturación y persistencia. También actuará como intermediario entre React y el chatbot.
3. **API de chatbot en FastAPI**
   Será un servicio construido en Python. Recibirá los mensajes desde el backend .NET, ejecutará LangGraph y LangChain, y consumirá la API de .NET cuando necesite datos reales del sistema.
4. **Base de datos PostgreSQL con pgvector**
   Guardará los datos del negocio y permitirá búsquedas semánticas de productos mediante vectores.

------

## 8. Flujo de comunicación entre tecnologías

El flujo recomendado será el siguiente:

1. El usuario escribe un mensaje desde la vista de chatbot en React.
2. React envía el mensaje al backend en ASP.NET Core.
3. El backend .NET envía el mensaje a la API de chatbot en FastAPI.
4. FastAPI recibe la petición y ejecuta el flujo conversacional de LangGraph.
5. LangGraph determina el paso actual de la conversación.
6. LangChain ejecuta herramientas cuando necesita consultar productos, validar stock o crear ventas.
7. Las herramientas de LangChain llaman a la API de .NET mediante HTTP.
8. La API de .NET consulta PostgreSQL y aplica las reglas de negocio.
9. .NET responde a FastAPI.
10. FastAPI devuelve una respuesta conversacional al backend .NET.
11. El backend .NET responde a React.
12. React muestra la respuesta al usuario.

Representación general:

- React se comunica únicamente con .NET.
- .NET se comunica con FastAPI.
- FastAPI ejecuta LangChain y LangGraph.
- LangChain consulta la API de .NET.
- .NET administra la lógica del negocio y consulta PostgreSQL con pgvector.

------

## 9. Consumo del chatbot mediante FastAPI

El chatbot será expuesto como una API usando FastAPI, pero será consumido exclusivamente por el backend en .NET.

El backend .NET será el encargado de exponer un endpoint hacia React.

### Endpoint sugerido en .NET

**POST /api/chat/message**

Este endpoint recibirá el identificador de sesión y el mensaje del usuario, y se encargará de comunicarse con FastAPI.

### Ejemplo de envío desde React hacia .NET

```json
{
  "sessionId": "session-001",
  "message": "Quiero comprar dos audífonos bluetooth"
}
```

### Ejemplo de comunicación interna .NET → FastAPI

```json
{
  "sessionId": "session-001",
  "message": "Quiero comprar dos audífonos bluetooth"
}
```

### Respuesta final hacia React

```json
{
  "response": "Encontré Audífonos Bluetooth X200. Hay 12 unidades disponibles. El total por 2 unidades es $170.000. ¿Deseas confirmar la compra?",
  "state": "WAITING_CONFIRMATION"
}
```

------

## 10. Ejemplo de confirmación de compra

Solicitud desde React hacia .NET:

```json
{
  "sessionId": "session-001",
  "message": "Sí, confirma la compra"
}
```

Respuesta final:

```json
{
  "response": "Compra realizada exitosamente. La venta fue registrada desde el chatbot y se generó la factura FAC-000001.",
  "state": "SALE_COMPLETED",
  "invoiceNumber": "FAC-000001",
  "saleOrigin": "CHATBOT"
}
```

------

## 11. Responsabilidad de FastAPI

FastAPI será usado para exponer el chatbot como un servicio HTTP.

Sus responsabilidades serán:

- Recibir mensajes desde el backend .NET.
- Ejecutar el flujo de LangGraph.
- Ejecutar el agente construido con LangChain.
- Mantener el estado de la conversación.
- Devolver respuestas al backend .NET.

FastAPI no será responsable de vender directamente, descontar inventario o generar facturas.

------

## 12. Responsabilidad de LangChain

LangChain será utilizado para construir la lógica del asistente inteligente y conectarlo con herramientas externas.

Permitirá:

- Buscar productos.
- Consultar inventario.
- Validar disponibilidad.
- Crear una venta.
- Consultar una factura.
- Ejecutar acciones controladas mediante herramientas conectadas al backend.

------

## 13. Responsabilidad de LangGraph

LangGraph controlará el flujo conversacional del chatbot, organizando el proceso de compra en pasos definidos.

Permitirá:

- Mantener contexto de la conversación.
- Definir estados del flujo.
- Controlar decisiones del chatbot.
- Evitar respuestas inconsistentes.

------

## 14. Responsabilidad de ASP.NET Core

ASP.NET Core será el núcleo del sistema.

Será responsable de:

- Gestionar productos, inventario, ventas y facturación.
- Exponer endpoints para React.
- Consumir la API de FastAPI.
- Validar reglas de negocio.
- Ejecutar operaciones transaccionales.
- Garantizar consistencia de datos.

------

## 18. Responsabilidad de React con TypeScript

React será la interfaz del sistema.

Será responsable de:

- Mostrar información al usuario.
- Permitir la gestión administrativa.
- Proveer una interfaz de chatbot.
- Consumir únicamente la API de .NET.

Nunca se comunicará directamente con FastAPI.

------

## 24. Módulo de chatbot

## Descripción

El chatbot será el componente inteligente del sistema.

Estará construido en Python usando LangChain y LangGraph, expuesto mediante FastAPI, pero será consumido únicamente por el backend .NET.

## Funcionalidades esperadas

- Recibir mensajes desde .NET.
- Procesar conversaciones.
- Consultar datos reales mediante la API .NET.
- Validar productos y stock.
- Guiar al usuario en el proceso de compra.
- Generar respuestas coherentes y contextualizadas.

------

## 28. Flujo principal de compra desde chatbot

1. Usuario escribe en React.
2. React envía mensaje a .NET.
3. .NET envía mensaje a FastAPI.
4. FastAPI ejecuta LangGraph.
5. LangChain consulta .NET.
6. .NET responde.
7. FastAPI responde a .NET.
8. .NET responde a React.

------

## 33. Pantallas sugeridas del frontend

El frontend deberá incluir varias vistas para cubrir tanto la administración como la interacción con el chatbot.

### Dashboard

Vista general del sistema donde se podrán visualizar métricas como:

- Total de productos.
- Ventas del día.
- Productos con bajo stock.
- Ventas realizadas por chatbot.
- Últimas facturas generadas.

### Productos

Vista para administrar productos.

Debe permitir:

- Crear productos.
- Editar productos.
- Activar o desactivar productos.
- Buscar productos.
- Visualizar categoría, precio y estado.

### Inventario

Vista para gestionar existencias.

Debe permitir:

- Consultar stock actual.
- Ver productos con bajo stock.
- Registrar ajustes manuales.
- Consultar movimientos de inventario.

### Ventas

Vista para consultar ventas.

Debe permitir:

- Ver listado de ventas.
- Filtrar por origen (manual o chatbot).
- Consultar detalle de cada venta.
- Ver estado de la venta.

### Facturas

Vista para consultar facturas.

Debe permitir:

- Ver listado de facturas.
- Consultar detalle.
- Buscar por número.
- Descargar factura (opcional).

### Chatbot

Vista conversacional donde el usuario interactúa con el asistente.

Debe permitir:

- Enviar mensajes.
- Recibir respuestas.
- Mostrar sugerencias de productos.
- Confirmar compras.
- Visualizar resultados de la operación.

Esta vista deberá consumir la API de .NET, no FastAPI directamente.

------

## 34. Fases del taller

## Fase 5: API del chatbot con FastAPI

Se crea el servicio consumido por .NET, no por React.

Incluye:

- Configuración de FastAPI.
- Endpoint de recepción de mensajes.
- Integración con LangChain y LangGraph.
- Manejo de sesiones.

------

## Fase 8: Frontend con React y TypeScript

Incluye:

- Construcción de vistas administrativas.
- Implementación del chatbot.
- Consumo de la API .NET.
- Manejo de estado del frontend.

------

## 36. Casos de prueba sugeridos

## Caso 7: Consumo del chatbot

- React envía mensaje a .NET.
- .NET envía mensaje a FastAPI.
- FastAPI procesa la solicitud.
- FastAPI responde a .NET.
- .NET responde a React.

Resultado esperado:

El usuario recibe una respuesta coherente basada en datos reales del sistema.

------

## 39. Entregables finales

1. Evidencia de React consumiendo la API .NET para el chatbot.
2. Evidencia de comunicación entre .NET y FastAPI.
3. Evidencia del flujo completo funcionando.

------

## 40. Resultado esperado final

Al finalizar el taller se espera:

- React consume chatbot vía .NET.
- .NET consume FastAPI.
- FastAPI ejecuta LangChain y LangGraph.
- Flujo desacoplado y controlado.
- Sistema completo funcionando de extremo a extremo.
