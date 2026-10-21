# EncodeAssistant — Bandeja conversacional

## Objetivo principal

Construir una bandeja web donde un operador recibe, visualiza y responde en tiempo real los mensajes de texto que los usuarios envían a un bot de Telegram.

## Stack tecnológico

| Área | Producto | Tecnología |
|---|---|---|
| Backend | Api Minimal - WebHook | .NET 10 |
| Frontend | App de mensajería para operadores centralizada | Angular última versión estable |
| Base de datos | Base de datos para mantener persistencia | SQL Server |
| Mensajería | Mensajería, colas de mensajes | RabbitMQ para el procesamiento de los mensajes entrantes |
| Tiempo real | Comunicación en tiempo real entre backend y frontend | SignalR (.NET) |
| Telegram | Bot propio (creado con BotFather) y webhook registrado en Telegram usando la URL pública de ngrok | Telegram BotFather |
| Exposición pública | ngrok para exponer el webhook y la app | ngrok |
| Ejecución | La solución levanta con `docker compose up` | Docker Compose |

---

## 1. Requisitos previos

| Requisito | Detalle |
|---|---|
| Docker Desktop (o Docker Engine + Compose v2) | Único requisito obligatorio para levantar toda la solución. Verificar con `docker compose version`. |
| Cuenta de Telegram + bot | Crear un bot con [@BotFather](https://t.me/BotFather): `/newbot` → te entrega el **token** (formato `123456:AA...`). |
| Dos cuentas de ngrok | Se usan **dos túneles con URL estática**, uno por servicio, cada uno con su propia cuenta: [dashboard.ngrok.com](https://dashboard.ngrok.com) → *Your Authtoken* y *Static Domains*. |
| (Opcional) SDK .NET 10 y Node.js 22+ | Solo si querés correr la API o el frontend fuera de Docker en desarrollo local. |

> ⚠️ Solo puede haber **un agente de ngrok por dominio**: si tenés un ngrok local corriendo con el mismo dominio que el contenedor, este falla con `ERR_NGROK_334`.

## 2. Cómo completar el `.env`

```bash
cp .env.example .env
# editar .env con tus valores reales
```

El `.env` **nunca se commitea** (lo ignora `.gitignore`); solo se versiona `.env.example`.

### Obligatorias (sin ellas `docker compose` no arranca)

| Variable | Qué poner | Cómo obtenerla |
|---|---|---|
| `SQLSERVER_SA_PASSWORD` | Password fuerte de `sa` (mín. 8 caracteres, con mayúscula, minúscula, número y símbolo) | La elegís vos. **Si cambiás un `.env` ya usado, recreá el volumen con `docker compose down -v`**: SQL Server solo aplica la contraseña el primer arranque. |
| `NGROK_AUTHTOKEN_API` | Authtoken de la cuenta de ngrok del túnel de la **API** | ngrok dashboard → *Your Authtoken* |
| `NGROK_DOMAIN_API` | Dominio estático del túnel de la **API** (sin `https://` ni barra final) | ngrok dashboard → *Static Domains* |
| `NGROK_AUTHTOKEN_APP` | Authtoken de la **segunda** cuenta de ngrok (túnel del frontend) | Igual que arriba, con la otra cuenta |
| `NGROK_DOMAIN_APP` | Dominio estático del túnel del **frontend** (sin `https://` ni barra final) | Igual que arriba |

### Recomendadas / de conexión entre servicios

| Variable | Qué poner |
|---|---|
| `TELEGRAM_BOT_TOKEN` | Token que entregó BotFather. Lo usan dos cosas: la API para **enviar las respuestas del operador** (`sendMessage`) y el `setWebhook` manual (ver §4). El compose ya se lo pasa a la API. |
| `TELEGRAM_WEBHOOK_SECRET` | Secreto propio (lo elegís vos) para validar el header `X-Telegram-Bot-Api-Secret-Token` cuando se implemente (Fase 5). |
| `PUBLIC_BASE_URL` | URL **completa** del túnel de la API, ej. `https://wielder-removing-dilation.ngrok-free.dev` (sin barra final). |
| `FRONTEND_PUBLIC_URL` | URL completa del túnel del frontend. |
| `CORS_ALLOWED_ORIGINS` | Orígenes permitidos separados por coma; debe incluir la URL pública del frontend (`FRONTEND_PUBLIC_URL`). La API lo aplica al middleware CORS y al *negotiate* de SignalR. |
| `FRONTEND_API_BASE_URL` / `FRONTEND_SIGNALR_URL` | URLs que el navegador usará para llegar a la API y a SignalR (HTTPS público para evitar *mixed content*). El contenedor del frontend genera `/assets/config.json` con estos valores al arrancar y la app los lee en runtime. |
| `RABBITMQ_*` | Ya vienen con valores de referencia; se usan cuando RabbitMQ entre al compose (Fase 3). |

> ⚠️ **Aviso de ngrok en el plan free (`ERR_NGROK_6024`).** ngrok intercepta las peticiones que vienen de un **navegador** y devuelve su página de advertencia ("You are about to visit…") en lugar de la respuesta de la API. Como `FRONTEND_API_BASE_URL` apunta al túnel, la bandeja vería esa página HTML en vez del JSON y el sidebar quedaría vacío (el error del navegador parece un problema de CORS, pero no lo es: `curl` funciona porque no lo intercepta). La app lo resuelve sola enviando el header `ngrok-skip-browser-warning` en todas las llamadas HTTP (`ngrokInterceptor`) y en el *negotiate* de SignalR; no hay que hacer nada.
>
> Si preferís no depender del túnel para el navegador: poné `FRONTEND_API_BASE_URL=http://localhost:5088` y `FRONTEND_SIGNALR_URL=http://localhost:5088/hubs/inbox` en el `.env`, reiniciá `frontend` (`docker compose up -d frontend`) y dejá el túnel solo para el webhook de Telegram.

## 3. Cómo levantar la solución
```bash
docker compose up --build      # levanta todo (usa el .env local)
docker compose ps              # estado de los servicios
docker compose logs -f api     # seguir los logs de la API
docker compose down            # detener
docker compose down -v         # detener y borrar los datos de SQL Server
```

Validar la configuración sin tocar tu `.env`:

```bash
docker compose --env-file .env.example config
```

Servicios que levanta el compose:

| Servicio | Puerto host | Qué es |
|---|---|---|
| `sqlserver` | `1433` | SQL Server 2022 (volumen `sqlserver-data`) |
| `api` | `5088` → `8080` | API .NET 10 (Swagger en Development) |
| `frontend` | `4200` → `80` | Bandeja Angular servida por nginx |
| `ngrok-api` | — | Túnel público → API (`https://<NGROK_DOMAIN_API>`) |
| `ngrok-app` | — | Túnel público → frontend (`https://<NGROK_DOMAIN_APP>`) |

URLs resultantes (reemplazá los dominios por los tuyos):

- API local: `http://localhost:5088` — Swagger: `http://localhost:5088/swagger`
- Bandeja local: `http://localhost:4200`
- API pública: `https://<NGROK_DOMAIN_API>/api/v1/health`
- Bandeja pública: `https://<NGROK_DOMAIN_APP>/`

> Desarrollo sin Docker: `dotnet run --project EncodeAssistant/encode-api/encode-api.csproj` (perfil `http`, puerto 5088). 
> Necesita `ConnectionStrings__SqlServer` en el entorno (la misma cadena que usa el compose) apuntando a tu SQL Server local.
> Para el frontend en dev: `cd EncodeAssistant/encondechatapp && npm start` (lee `public/assets/config.json`).

## 4. Cómo probarla de punta a punta

Referencia rápida de la API (Swagger en `http://localhost:5088/swagger`):

| Método | Ruta | Qué hace |
|---|---|---|
| `GET` | `/api/v1/health` | Healthcheck. |
| `POST` | `/api/telegram/webhook` | Recibe el `Update` de Telegram, persiste el mensaje y lo emite por SignalR. `/start` se descarta (ver Paso 3). |
| `GET` | `/api/v1/contacts` | Contactos del sidebar, ordenados por último mensaje. |
| `GET` | `/api/v1/contacts/{chatId}/messages` | Historial de la conversación, en orden cronológico. |
| `POST` | `/api/v1/contacts/{chatId}/messages` | Respuesta del operador: se persiste, se envía al usuario por la Bot API de Telegram y se emite por SignalR. |
| `POST` | `/api/v1/contacts/{chatId}/read` | Marca la conversación como leída (no leídos en 0). |
| `DELETE` | `/api/v1/contacts/{chatId}` | Elimina una conversación completa (contacto + mensajes). |
| `DELETE` | `/api/v1/contacts` | Vacía la bandeja. Devuelve `{"eliminadas": n}`. |
| `DELETE` | `/api/v1/messages/{id}` | Elimina un mensaje suelto y recalcula la vista previa del contacto. |

Eventos que emite el hub SignalR (`/hubs/inbox`): `NuevoMensaje`, `ContactoActualizado` y `ConversacionEliminada` (con el `chatId`, o vacío al vaciar la bandeja).

**Paso 1 — Salud del servicio (local y público)**

```bash
curl http://localhost:5088/api/v1/health
# → {"status":"Healthy"}

curl https://<NGROK_DOMAIN_API>/api/v1/health
# → {"status":"Healthy"}   (verificado 20/10/2026)
```

**Paso 2 — Explorar los endpoints en Swagger**

Abrí `http://localhost:5088/swagger` (solo en `ASPNETCORE_ENVIRONMENT=Development`, que es el default del compose).

**Paso 3 — Simular un mensaje entrante de Telegram**

```bash
curl -X POST http://localhost:5088/api/telegram/webhook \
  -H "Content-Type: application/json" \
  -d '{
    "update_id": 100001,
    "message": {
      "message_id": 1,
      "from": { "id": 123456789, "is_bot": false, "first_name": "Ana", "last_name": "Gómez", "username": "anagomez" },
      "chat": { "id": 123456789, "type": "private" },
      "text": "hola"
    }
  }'
# → 200 OK
```

Y ver el registro en los logs:

```bash
docker compose logs -f api | grep "Mensaje entrante"
# → Mensaje entrante | ChatId: 123456789 | Tipo: private | Emisor: @anagomez | Texto: hola
```

También podés hacerlo desde `EncodeAssistant/encode-api/encode-api.http` (VS Code / Rider / Swagger).

> **Sobre `/start`:** es el comando con el que el usuario conecta el bot, no un mensaje de la conversación. La API lo registra en el log, pero **no** lo persiste, **no** lo emite por SignalR y **no** crea el contacto: por eso no aparece en la bandeja. El contacto se crea cuando llega el primer mensaje real. Si probás con `"text": "/start"` vas a ver el log y ningún cambio en `GET /api/v1/contacts`.

El mensaje queda **persistido en SQL Server** y se emite por SignalR:

```bash
curl http://localhost:5088/api/v1/contacts
# → [{"id":"123456789","name":"Ana Gómez","username":"anagomez","lastMessage":"hola","lastMessageAt":"…Z","unreadCount":1,"isActive":true}]
```

**Paso 4 — Probar el camino completo del webhook sin la app de Telegram**

Este paso simula lo mismo que el Paso 3, pero atravesando el túnel público de ngrok: es el mismo camino que recorre un update real de Telegram, sin necesitar la app ni el bot.

```bash
set -a; source .env; set +a   # carga NGROK_DOMAIN_API y TELEGRAM_BOT_TOKEN

curl -s -o /dev/null -w "HTTP %{http_code}\n" -X POST \
  "https://${NGROK_DOMAIN_API}/api/telegram/webhook" \
  -H "Content-Type: application/json" \
  -d '{
    "update_id": 999999,
    "message": {
      "message_id": 1,
      "from": { "id": 123456789, "is_bot": false, "first_name": "Ana", "last_name": "Gómez", "username": "anagomez" },
      "chat": { "id": 123456789, "type": "private" },
      "text": "test-tunel"
    }
  }'
# → HTTP 200
```

Y confirmá en los logs que el update llegó y se procesó:

```bash
docker compose logs -f api | grep "Mensaje entrante"
# → Mensaje entrante | ChatId: 123456789 | Tipo: private | Emisor: @anagomez | Texto: test-tunel
```

Si el curl da 200 y el log aparece, el camino ngrok → API → webhook funciona: cualquier update que envíe Telegram va a llegar igual. `getWebhookInfo` (Paso 5) es el complemento para depurar el lado de Telegram: muestra la URL registrada, los updates pendientes y los errores recientes.

**Paso 5 — Probar con el bot real en Telegram (de punta a punta)**

Registrar el webhook apuntando al túnel público de la API:

```bash
TOKEN=$(grep '^TELEGRAM_BOT_TOKEN=' .env | cut -d= -f2)
curl -s -F "url=https://<NGROK_DOMAIN_API>/api/telegram/webhook" \
  "https://api.telegram.org/bot${TOKEN}/setWebhook"

# Verificar el registro:
curl -s "https://api.telegram.org/bot${TOKEN}/getWebhookInfo"
```

> Por ahora el registro es **manual**; el registro automático al levantar la solución se implementa en la Fase 5.

Entonces, desde Telegram: buscá tu bot, mandale `/start` y después un texto. Con la bandeja abierta (Paso 7), el mensaje tiene que **aparecer solo**, sin recargar la página.

**Paso 6 — Responder desde la bandeja (persistencia + envío real a Telegram)**

La respuesta del operador se guarda **y viaja al usuario por la Bot API de Telegram**:

```bash
curl -X POST http://localhost:5088/api/v1/contacts/123456789/messages \
  -H "Content-Type: application/json" \
  -d '{"text": "Hola desde la bandeja"}'
# → 201 {"id":2,"contactId":"123456789","text":"Hola desde la bandeja","direction":"outgoing",
#         "delivered":true,"deliveryError":null,…}
```

Usá el `chatId` **real** del contacto (el mismo `id` que devuelve `GET /api/v1/contacts`); si probás con un chat inexistente, Telegram rechaza el envío:

```bash
curl -X POST http://localhost:5088/api/v1/contacts/999999/messages \
  -H "Content-Type: application/json" -d '{"text": "hola"}'
# → 201 {"delivered":false,"deliveryError":"Bad Request: chat not found",
#         "advertencia":"El mensaje se guardó, pero Telegram no lo entregó: …"}
```

El mensaje se persiste **siempre**, incluso si Telegram lo rechaza, para no perder el registro de la conversación: en ese caso queda con `delivered: false` y el motivo en `deliveryError`, y la respuesta trae una `advertencia`. Se devuelve `201` igual para que el mensaje no se descarte.

El historial completo queda en la base; al reabrir la conversación se ve igual:

```bash
curl http://localhost:5088/api/v1/contacts/123456789/messages
curl -X POST http://localhost:5088/api/v1/contacts/123456789/read   # abre y marca leídos
```

> Cada mensaje saliente guarda además el `telegramMessageId` que devolvió Telegram al aceptarlo, y `delivered: null` en los entrantes (donde no aplica).

**Paso 6 bis — Borrar conversaciones (también desde la UI)**

Desde el código:

```bash
# Elimina una conversación completa (contacto + mensajes)
curl -X DELETE http://localhost:5088/api/v1/contacts/123456789
# → 204 No Content (404 si el contacto no existe)

# Vacía la bandeja entera
curl -X DELETE http://localhost:5088/api/v1/contacts
# → {"eliminadas":4}
```

Desde la app:

- **Vaciar** (cabecera del sidebar) borra todas las conversaciones.
- El **ícono de papelera** de cada tarjeta borra esa conversación.
- **Eliminar** (cabecera del chat) borra la conversación abierta.

Las tres acciones piden **confirmación** en un diálogo modal (que además se cierra con Escape o con **Cancelar**); recién al confirmar se llama a la API. Las tres emiten `ConversacionEliminada` por SignalR, así que las demás pestañas abiertas se actualizan solas.

> Limpiar la bandeja **no** borra el historial del lado de Telegram ni desregistra el webhook: las conversaciones vuelven a aparecer cuando el usuario le escribe de nuevo al bot. Para empezar de cero también la base, `docker compose down -v` recrea el volumen de SQL Server.

**Paso 7 — Abrir la bandeja**

- Local: `http://localhost:4200` — Pública: `https://<NGROK_DOMAIN_APP>/`
- Vas a ver el layout de dos paneles: a la izquierda la lista de conversaciones (icono de usuario, nombre o @usuario, vista previa, hora y contador de no leídos; la más reciente arriba) y a la derecha la conversación abierta.
- Al hacer clic en un contacto se carga el historial y se marcan los mensajes como leídos.
- Con la conversación abierta, mandá otro mensaje al bot (Paso 3 o 5): aparece al instante y el sidebar se reordena.
- Escribí en la caja de abajo y presioná Enter: la respuesta se guarda y se muestra como burbuja propia.
- Para borrar: **Vaciar** en la cabecera del sidebar (todas las conversaciones), el ícono de papelera de una tarjeta (esa conversación) o **Eliminar** en la cabecera del chat (la abierta). Las tres piden confirmación antes de borrar.

## 5. Supuestos

Decisiones tomadas ante lo que la spec no define:

1. **Angular 22 (no LTS):** la spec pide la "última versión estable"; se usa 22 en lugar de 21 LTS.
2. **Dos cuentas de ngrok:** se exige un túnel para el webhook y otro para la app; ngrok permite un solo agente por dominio, y gratis ofrece un dominio estático por cuenta → dos cuentas = dos authtokens (`NGROK_AUTHTOKEN_API` / `NGROK_AUTHTOKEN_APP`).
3. **Estado actual del flujo:** el webhook recibe el `Update` real de Telegram, lo persiste en SQL Server (EF Core) y lo emite por SignalR a la bandeja Angular, que lista contactos, muestra el historial, marca leídos, permite responder y borrar conversaciones (individual o toda la bandeja, con confirmación). Las respuestas del operador **viajan al usuario por la Bot API de Telegram** (con estado de entrega: `delivered`, `deliveryError`, `telegramMessageId`). Falta: RabbitMQ entre el webhook y la persistencia (Fase 3) y el registro automático del webhook al arrancar (Fase 5, hoy es manual). SQL Server ya corre en el compose; las variables de RabbitMQ están declaradas en `.env`, pero **el servicio RabbitMQ todavía no está en el compose**.
4. **Registro manual del webhook:** el auto-registro al arrancar todavía no está implementado; por ahora se registra con `setWebhook` (ver §4, paso 5).
5. **Envío a Telegram sin bloquear la respuesta:** si la Bot API rechaza el mensaje (chat inexistente, bot bloqueado, token inválido o timeout), la API **no devuelve error**: guarda el mensaje igual, lo marca con `delivered: false` y el motivo en `deliveryError`, y responde `201` con una `advertencia`. Así la conversación nunca queda incompleta.
5. **Fuera de alcance** (según spec): autenticación/usuarios, mensajes no textuales, diseño responsive y paginación del historial.
6. **Puertos por defecto:** API `5088`, frontend `4200`, SQL Server `1433` (solo para desarrollo local; en producción no se publican).
7. **Esquema sin migraciones:** la base se crea con `EnsureCreated()` al arrancar (no hay migraciones de EF Core todavía). Es suficiente para la prueba, pero un cambio de modelo no se aplica sobre una base existente: hay que recrear el volumen (`docker compose down -v`). Las migraciones entran junto con la separación en capas (Fase 1).
8. **`/start` no se muestra:** es el comando de conexión del bot, no un mensaje del usuario. Se registra en el log y se descarta; el contacto se crea con el primer mensaje real (ver §4, paso 3).
