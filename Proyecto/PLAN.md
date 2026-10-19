# Plan de trabajo — Bandeja conversacional (encodelabs)

Documento vivo. Se actualiza a medida que se implementan funcionalidades: marcar `[x]` al completar y mantener la sección **Estado actual** al día.

Fuente de requisitos: `Proyecto/chat_inbox_tech_test (5).md`. Convenciones y stack: `README.md` y `AGENTS.md`.

## Objetivo

Bandeja web donde un operador recibe, visualiza y responde **en tiempo real** los mensajes de texto que los usuarios envían a un bot de Telegram. Un solo `docker compose up` levanta toda la solución.

## Alcance

**Dentro:**
1. Recepción en tiempo real de mensajes de texto del bot.
2. Listado de contactos con nombre/username, cantidad de no leídos y vista previa del último mensaje.
3. Abrir conversación y ver historial.
4. Marcar mensajes como leídos al abrir la conversación.
5. Responder desde la bandeja; la respuesta llega al usuario por Telegram.

**Fuera (no implementar):** autenticación/usuarios, mensajes no textuales, diseño responsive, paginación del historial.

## Arquitectura

**Backend — Clean Architecture** (obligatorio, criterio evaluado). Proyectos propuestos dentro de `EncodeAssistant/`:

| Proyecto | Responsabilidad |
|---|---|
| `Encode.Domain` | Entidades (`Contact`, `Conversation`, `Message`) y reglas de negocio. Sin dependencias. |
| `Encode.Application` | Casos de uso / interfaces (repositorios, cliente de Telegram, bus de mensajes). |
| `Encode.Infrastructure` | EF Core + SQL Server, RabbitMQ, cliente de Telegram, servicios externos. |
| `encode-api` | Host ASP.NET Core: webhook de Telegram, endpoints REST, hub de SignalR, registro del webhook al arrancar. |

**Frontend:** app Angular (última estable) en su propio contenedor, servida como SPA.

**Flujo de mensajes (a detallar en ADR-003):**
`Usuario → Telegram → webhook (ngrok) → encode-api → RabbitMQ → consumer → SQL Server → SignalR → operador`.
Respuesta: `operador → SignalR/HTTP → encode-api → Telegram Bot API → usuario`.

## Stack y decisiones

- Backend: **.NET 10** (SDK verificado 10.0.402).
- Frontend: **Angular** última estable.
- Base de datos: **SQL Server** (ADR-001).
- Mensajería: **RabbitMQ** (mensajes entrantes).
- Tiempo real: **SignalR** (ADR-002).
- Exposición pública: **ngrok**, webhook registrado automáticamente al levantar usando la URL pública.
- Ejecución: **Docker Compose**.

## Variables de entorno

Definidas en `.env.example` (raíz del repo) agrupadas por servicio:

- **Compose:** `COMPOSE_PROJECT_NAME`.
- **Telegram:** `TELEGRAM_BOT_TOKEN`, `TELEGRAM_WEBHOOK_SECRET`, `TELEGRAM_API_BASE_URL`.
- **ngrok:** `NGROK_AUTHTOKEN`, `NGROK_REGION`, `NGROK_DOMAIN`, `PUBLIC_BASE_URL`.
- **API:** `ASPNETCORE_ENVIRONMENT`, `API_HTTP_PORT`, `API_HTTPS_PORT`, `CORS_ALLOWED_ORIGINS`.
- **Frontend:** `FRONTEND_PORT`, `FRONTEND_API_BASE_URL`, `FRONTEND_SIGNALR_URL`.
- **SQL Server:** `SQLSERVER_HOST`, `SQLSERVER_PORT`, `SQLSERVER_DB`, `SQLSERVER_USER`, `SQLSERVER_SA_PASSWORD`, `SQLSERVER_CONNECTION_STRING`.
- **RabbitMQ:** `RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_MANAGEMENT_PORT`, `RABBITMQ_DEFAULT_USER`, `RABBITMQ_DEFAULT_PASS`, `RABBITMQ_VHOST`, `RABBITMQ_INCOMING_QUEUE`.

`.env` **no** se commitea (lo ignora `.gitignore`). Solo se versiona `.env.example` con estos nombres y valores falsos.

## Fases

### Fase 0 — Andamiaje y seguridad del repo
- [x] Crear `.gitignore` raíz que ignore `.env`, `bin/`, `obj/`, `node_modules/`, artefactos de build.
- [x] Crear `.env.example` con las variables de arriba (valores falsos).
- [ ] Commit inicial del andamiaje.

### Fase 1 — Backend: estructura Clean Architecture
- [ ] Crear proyectos `Encode.Domain`, `Encode.Application`, `Encode.Infrastructure`.
- [ ] Registrar los 4 proyectos en `EncodeAssistant.slnx` y definir referencias entre capas.
- [ ] Modelar entidades del dominio y casos de uso (ingesta de mensaje, listar contactos, historial, marcar leído, responder).

### Fase 2 — Persistencia (SQL Server + EF Core)
- [ ] `DbContext`, configuraciones y migraciones.
- [ ] Repositorios en `Encode.Infrastructure`.
- [ ] Definir política de no leídos por contacto.

### Fase 3 — Mensajería (RabbitMQ)
- [ ] Publicar el mensaje entrante del webhook a una cola.
- [ ] Consumer que persiste y notifica.
- [ ] Definir topología de colas/exchanges (insumo de ADR-003).

### Fase 4 — Tiempo real (SignalR)
- [ ] Hub y contratos de notificación (nuevo mensaje, actualización de contacto).
- [ ] Emisión tras procesar el consumer.

### Fase 5 — Integración Telegram
- [ ] Cliente de la Bot API para enviar respuestas.
- [ ] Endpoint de webhook con validación del secret.
- [ ] Registro automático del webhook contra la URL pública al arrancar.

### Fase 6 — Frontend Angular
- [ ] Estructura de la app y layout de dos paneles.
- [ ] Lista de contactos (nombre, no leídos, preview).
- [ ] Vista de conversación + historial + campo de respuesta.
- [ ] Conexión SignalR y marcado de leídos al abrir.
- [ ] Estilo alineado a encodelabs.com.ar.

### Fase 7 — Docker y exposición
- [ ] Dockerfiles multi-stage para API y frontend (usuario no root, imágenes livianas).
- [ ] `docker-compose.yml` con api, frontend, SQL Server, RabbitMQ y ngrok.
- [ ] Healthchecks, redes, volúmenes y dependencias (`depends_on` con condición healthy).
- [ ] Verificar `docker compose up` de punta a punta.

### Fase 8 — Documentación y entregables
- [ ] `README.md` raíz con las 6 secciones que pide la spec (requisitos, `.env`, cómo levantar, cómo probar, supuestos).
- [ ] Diagramas C4 Nivel 1 y 2 en `Proyecto/AnalisisC4/` (SVG o XML de draw.io).
- [ ] ADR-001 (base de datos), ADR-002 (tiempo real), ADR-003 (flujo del webhook + topología RabbitMQ).
- [ ] Documento de una página "Trabajo con IA".

### Fase 9 — Tests y extras
- [ ] Tests unitarios sobre el dominio.
- [ ] Respuestas a las preguntas bonus del documento de requisitos.

## Entregables (checklist de aceptación)

- [ ] `docker-compose.yml` funcional.
- [x] `.env.example` sin valores reales.
- [ ] `README.md` completo.
- [ ] C4 Nivel 1 y 2.
- [ ] ADR-001, ADR-002, ADR-003.
- [ ] Documento de trabajo con IA.
- [ ] Repositorio sin `.env` ni secretos.

## Estado actual

- Andamiaje inicial: solo existe `encode-api` (minimal API .NET 10 con `GET /health`) y `EncodeAssistant.slnx` vacío.
- Fase 0 casi completa: `.gitignore` raíz y `.env.example` (variables con datos falsos) creados. Falta el commit inicial.
- Git en `main` sin commits; todo sin trackear.
- Pendiente: Fases 1 a 9.
