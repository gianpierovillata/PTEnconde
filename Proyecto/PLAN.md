# Plan de trabajo — Bandeja conversacional (encodelabs)

Documento vivo. Se actualiza a medida que se implementan funcionalidades: marcar `[x]` al completar y mantener la sección **Estado actual** al día.

Fuente de requisitos: `Proyecto/chat_inbox_tech_test (5).md`. Convenciones y stack: `README.md` y `AGENTS.md`.

## Objetivo

Bandeja web donde un operador recibe, visualiza y responde **en tiempo real** los mensajes de texto que los usuarios envían a un bot de Telegram. Un solo `docker compose up` levanta toda la solución (API, frontend Angular, SQL Server, RabbitMQ y ngrok), configurada mediante `.env`.

## Alcance

**Dentro:**
1. Recepción en tiempo real de mensajes de texto del bot (webhook de Telegram → API → RabbitMQ → consumer → SignalR).
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

**Frontend:** app Angular **22** (última versión estable) en su propio contenedor, servida como SPA (nginx en producción).

**Flujo de mensajes (a detallar en ADR-003):**
`Usuario → Telegram → webhook (ngrok) → encode-api → RabbitMQ → consumer → SQL Server → SignalR → operador`.
Respuesta: `operador → SignalR/HTTP → encode-api → Telegram Bot API → usuario`.

## Stack y decisiones

- Backend: **.NET 10** (SDK verificado 10.0.402).
- Frontend: **Angular 22** (`@angular/cli@22`, proyecto ya generado con `^22.2.0`). La spec pide la **última versión estable**: se usa 22 (aunque no es LTS) en lugar de 21 LTS; registrar en **Supuestos** del README. Coherente con `PLAN_FRONTEND.md`.
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

## Entorno verificado (19/10/2026)

| Herramienta | Estado | Versión / nota |
|---|---|---|
| .NET SDK | ✅ instalado | 10.0.402 — `dotnet build/run --project EncodeAssistant/encode-api/encode-api.csproj` |
| Node.js | ✅ instalado | v24.22.0 |
| npm | ✅ instalado | 11.19.1 — **en PowerShell usar `npm.cmd`** (la política de ejecución bloquea `npm.ps1`); o `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| Angular CLI | ❌ no instalado | Instalar con `npm.cmd install -g @angular/cli@22` (coherente con la app ya generada en 22.2.x) |
| Docker Desktop | ✅ instalado y corriendo | Engine 29.9.1, Compose v5.6.0 — **`docker` no está en el PATH de esta sesión**: usar `$env:PATH += "$env:LOCALAPPDATA\Programs\DockerDesktop\resources\bin"` o reabrir la terminal |
| ngrok | ❌ no instalado | Definir si va como servicio del compose (imagen `ngrok/ngrok`) o binario local — ver Fase 7 |
| Git | ✅ | rama `main`, árbol limpio, 3 commits, `origin` configurado |

**Discrepancias corregidas (20/10/2026):**
- ✅ `AGENTS.md` actualizado: ya no dice "sin commits todavía" (hay 6) y refleja el `.slnx` con proyecto.
- ✅ `Program.cs` sin `WeatherForecast`; `encode-api.http` actualizado a los endpoints reales.
- ✅ Namespaces del dominio normalizados a `EncodeAssistant.EncodeApi.*`; `RootNamespace` del csproj corregido.
- ✅ `EncodeAssistant.slnx` ahora referencia `encode-api`.

**Discrepancias pendientes (se resuelven en fases posteriores):**
- Carpetas `Aplication/` (falta la "p") y services vacíos dentro del host: mover a las capas de Clean Architecture en Fase 1.
- `Proyecto/AnalisisC4/Nivel1.drawio` y `Nivel2.drawio` existen: revisar que coincidan con la arquitectura final antes de entregar.

## Checklist transversal de seguridad y despliegue

Aplica a todas las fases (revisar antes de cada commit):

- [ ] `.env` jamás se commitea (ya cubierto por `.gitignore`); solo `.env.example` con valores falsos.
- [ ] Secretos solo vía variables de entorno; nunca hardcodeados en `appsettings.json`, código ni imágenes.
- [ ] Webhook de Telegram validado con `X-Telegram-Bot-Api-Secret-Token` (`TELEGRAM_WEBHOOK_SECRET`).
- [ ] Contenedores con **usuario no root** (API ya lo hace); verificar para frontend y servicios nuevos.
- [ ] SQL Server y RabbitMQ **no publicados en el host en producción** (solo red interna del compose); el puerto 1433 expuesto es solo para dev local.
- [ ] Credenciales fuertes y distintas por servicio; usuario RabbitMQ dedicado con permisos mínimos sobre su vhost/cola.
- [ ] CORS restringido con `CORS_ALLOWED_ORIGINS` (sin `AllowAnyOrigin` en producción).
- [ ] ngrok expone solo el endpoint del webhook, no la API completa.
- [ ] `.dockerignore` en cada contexto de build (evitar `bin/`, `obj/`, `node_modules/`, `.env` dentro de la imagen).
- [ ] Imágenes con versiones fijadas (no `latest`), `healthchecks` en compose y `depends_on` con condición de salud.
- [ ] HTTPS/redirects y headers de seguridad en el frontend servido por nginx.
- [ ] Repo final sin secretos: revisar historial de git antes de publicar.

## Fases

### Fase 0 — Andamiaje y seguridad del repo
- [x] Crear `.gitignore` raíz que ignore `.env`, `bin/`, `obj/`, `node_modules/`, artefactos de build.
- [x] Crear `.env.example` con las variables de arriba (valores falsos).
- [x] Commit inicial del andamiaje (`085b43b`).

### Fase 0.5 — Preparación del entorno (previo a código)
- [ ] Instalar Angular CLI 22: `npm.cmd install -g @angular/cli@22` y verificar `ng version`.
- [ ] Resolver PATH de Docker en la shell (reabrir terminal o agregar `resources\bin` al PATH).
- [ ] Decidir y dejar instalado ngrok (servicio en compose recomendado vs. binario local).
- [ ] Completar `.env` local con token real del bot (BotFather) y token de ngrok (no commitear).

### Fase 1 — Backend: estructura Clean Architecture
- [ ] Crear proyectos `Encode.Domain`, `Encode.Application`, `Encode.Infrastructure`.
- [ ] Registrar los 4 proyectos en `EncodeAssistant.slnx` y definir referencias entre capas.
- [ ] Mover `Domain/Entities` y services al lugar correcto; renombrar `Aplication` → `Application` y unificar namespaces.
- [ ] Eliminar código de plantilla sobrante (`WeatherForecast`, `encode-api.http` viejo).
- [ ] Modelar entidades del dominio (`Usuario`/Contacto, `Mensaje`, `Conversación`, `Operador`) y casos de uso (ingesta de mensaje, listar contactos, historial, marcar leído, responder).

### Fase 2 — Persistencia (SQL Server + EF Core)
- [ ] `DbContext`, configuraciones y migraciones.
- [ ] Repositorios en `Encode.Infrastructure`.
- [ ] Definir política de no leídos por contacto.
- [ ] Confirmar que la base `EncodeInbox` se crea por migración al levantar (o `EnsureCreated` en dev).

### Fase 3 — Mensajería (RabbitMQ)
- [ ] Sumar RabbitMQ al `docker-compose.yml` (credenciales desde `.env`, red interna, healthcheck).
- [ ] Publicar el mensaje entrante del webhook a una cola.
- [ ] Consumer que persiste y notifica.
- [ ] Definir topología de colas/exchanges (insumo de ADR-003).

### Fase 4 — Tiempo real (SignalR)
- [ ] Hub (`/hubs/inbox`) y contratos de notificación (nuevo mensaje, actualización de contacto).
- [ ] Emisión tras procesar el consumer.
- [ ] CORS para el origen del frontend.

### Fase 5 — Integración Telegram
- [ ] Cliente de la Bot API para enviar respuestas.
- [ ] Endpoint de webhook con validación del secret (`X-Telegram-Bot-Api-Secret-Token`).
- [ ] Registro automático del webhook contra la URL pública al arrancar (reintento si ngrok aún no está listo).

### Fase 6 — Frontend Angular (22)
- [ ] Scaffold con `ng new` (standalone, sin SSR salvo que se necesite) dentro de `EncodeAssistant/frontend/` (o raíz equivalente).
- [ ] Estructura de la app y layout de dos paneles.
- [ ] Lista de contactos (nombre, no leídos, preview).
- [ ] Vista de conversación + historial + campo de respuesta.
- [ ] Conexión SignalR y marcado de leídos al abrir.
- [ ] Estilo alineado a encodelabs.com.ar.
- [ ] `environment` con `FRONTEND_API_BASE_URL` / `FRONTEND_SIGNALR_URL` inyectadas por build args o runtime config.

### Fase 7 — Docker y exposición
- [x] Dockerfile multi-stage para la API (.NET 10, usuario no root, `ASPNETCORE_URLS=http://+:8080`).
- [x] Dockerfile para SQL Server (imagen oficial 2022, `ACCEPT_EULA` + `Developer`).
- [x] `docker-compose.yml` con `api` + `sqlserver` (red `backend` y volumen `sqlserver-data`).
- [x] Verificado: `docker compose up` levanta `api` y `sqlserver`; `/health` responde 200 y SQL Server acepta consultas.
- [ ] Sumar RabbitMQ al compose.
- [ ] Sumar el frontend (Angular) al compose (build multi-stage node→nginx, usuario no root, `.dockerignore`).
- [ ] Sumar ngrok al compose y exponer solo el webhook con URL pública.
- [ ] Healthchecks + `depends_on` con condición en todos los servicios; `docker compose up` completo de punta a punta.

### Fase 8 — Documentación y entregables
- [ ] `README.md` raíz con las 6 secciones que pide la spec (requisitos previos, `.env`, cómo levantar, cómo probar, supuestos).
- [ ] Revisar/actualizar diagramas C4 Nivel 1 y 2 en `Proyecto/AnalisisC4/` (SVG o XML de draw.io).
- [ ] ADR-001 (base de datos), ADR-002 (tiempo real), ADR-003 (flujo del webhook + topología RabbitMQ).
- [ ] Documento de una página "Trabajo con IA".
- [ ] Actualizar `AGENTS.md` (commits, comandos nuevos, servicios del compose) y `README.md` de la raíz.
- [ ] Respuestas a las preguntas bonus (opcional, puntos extra).

### Fase 9 — Tests y extras
- [ ] Tests unitarios sobre el dominio (único tipo de tests que admite la spec).
- [ ] Revisión final de seguridad: historial de git sin `.env`, sin secretos en imágenes ni logs.

## Entregables (checklist de aceptación)

- [ ] `docker-compose.yml` funcional con **todos** los servicios (api, frontend, sqlserver, rabbitmq, ngrok).
- [x] `.env.example` sin valores reales.
- [ ] `README.md` completo (6 secciones).
- [ ] C4 Nivel 1 y 2.
- [ ] ADR-001, ADR-002, ADR-003.
- [ ] Documento de trabajo con IA.
- [ ] Repositorio sin `.env` ni secretos.

## Estado actual (20/10/2026)

- Git limpio en `main` con 6 commits (`085b43b` … `2c5872c`) e `origin` configurado.
- **Backend:** `encode-api` (minimal API .NET 10) con endpoints placeholder (`GET /`, `GET /api/v1/health`, `POST /api/v1/webhook/receiveMessage`, `POST /api/v1/webhook/sendMessage`). Entidades borrador (`Usuario`, `Mensaje`, `Operador`) dentro del host; services `MensajeService`/`UsuarioService` vacíos. Pendiente: estructura Clean Architecture (Fase 1).
- **Frontend:** `encondechatapp` (Angular 22 + SSR) con feature `inbox` (sidebar + chat + chat-header + message-list + message-input), tokens de diseño y routing listos; servicios/modelos en stub. Dockerizado (nginx, SPA). Pendiente: lógica real (Fases 4–6).
- **Docker:** `docker compose up` levanta `api` + `sqlserver` + `frontend`. Verificado `/health` 200 y SQL Server operativo. La base `EncodeInbox` la creará EF Core en la Fase 2. Faltan `rabbitmq`, `ngrok`, healthchecks y `.dockerignore`.
- **Inconsistencias corregidas hoy:** `AGENTS.md`, `PLAN_FRONTEND.md`, `Program.cs`, `encode-api.http`, namespaces del dominio y `EncodeAssistant.slnx` (ya referencia `encode-api`).
- Angular CLI y ngrok **no instalados** (Fase 0.5). Docker funciona pero su PATH no está en la sesión de shell actual.
- Diagramas C4 (Nivel 1 y 2) creados en draw.io; pendientes de revisión contra la arquitectura final.
- Pendiente: Fases 0.5 y 1 a 9.
