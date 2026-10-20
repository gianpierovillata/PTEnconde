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
| `TELEGRAM_BOT_TOKEN` | Token que entregó BotFather. Hoy se usa para registrar el webhook a mano (ver §4); la API aún no la recibe desde el compose. |
| `TELEGRAM_WEBHOOK_SECRET` | Secreto propio (lo elegís vos) para validar el header `X-Telegram-Bot-Api-Secret-Token` cuando se implemente (Fase 5). |
| `PUBLIC_BASE_URL` | URL **completa** del túnel de la API, ej. `https://wielder-removing-dilation.ngrok-free.dev` (sin barra final). |
| `FRONTEND_PUBLIC_URL` | URL completa del túnel del frontend. |
| `CORS_ALLOWED_ORIGINS` | Orígenes permitidos separados por coma; debe incluir la URL pública del frontend (`FRONTEND_PUBLIC_URL`). Hoy llega al contenedor de la API pero **el middleware CORS todavía no está implementado** (Fase 4). |
| `FRONTEND_API_BASE_URL` / `FRONTEND_SIGNALR_URL` | URLs que el navegador usará para llegar a la API y a SignalR (HTTPS público para evitar *mixed content*). Hoy se inyectan al contenedor del frontend; **la app Angular todavía no las consume** (servicios en stub, Fases 4–6). |
| `RABBITMQ_*` | Ya vienen con valores de referencia; se usan cuando RabbitMQ entre al compose (Fase 3). |

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

## 4. Cómo probarla de punta a punta

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
      "chat": { "id": 123456789, "type": "private" },
      "text": "hola"
    }
  }'
# → 200 OK
```

Y ver el registro en los logs:

```bash
docker compose logs -f api | grep "Mensaje recibido"
# → Mensaje recibido de ChatID 123456789: hola
```

También podés hacerlo desde `EncodeAssistant/encode-api/encode-api.http` (VS Code / Rider / Swagger).

Para probar la rama del comando de inicio, cambiá `"text": "hola"` por `"text": "/start"`.

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
      "chat": { "id": 123456789, "type": "private" },
      "text": "test-tunel"
    }
  }'
# → HTTP 200
```

Y confirmá en los logs que el update llegó y se procesó:

```bash
docker compose logs -f api | grep "Mensaje recibido"
# → Mensaje recibido de ChatID 123456789: test-tunel
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

Entonces, desde Telegram: buscá tu bot, mandale `/start` y después un texto. Debe aparecer en los logs de la API (`docker compose logs -f api`).

**Paso 6 — Probar la respuesta de la bandeja (placeholder)**

```bash
curl -X POST http://localhost:5088/api/v1/webhook/sendMessage \
  -H "Content-Type: application/json" \
  -d '{"chatId": "123456789", "text": "Hola desde la bandeja"}'
# → {"status":"Message sent"}   (la llamada real a la Bot API llega en la Fase 5)
```

**Paso 7 — Abrir la bandeja**

- Local: `http://localhost:4200` — Pública: `https://<NGROK_DOMAIN_APP>/`
- Verás el layout de dos paneles (lista de contactos + conversación). La UI es el esqueleto funcional: la lógica de contactos, historial y tiempo real (SignalR) se conecta en las Fases 4–6.

## 5. Supuestos

Decisiones tomadas ante lo que la spec no define:

1. **Angular 22 (no LTS):** la spec pide la "última versión estable"; se usa 22 en lugar de 21 LTS.
2. **Dos cuentas de ngrok:** se exige un túnel para el webhook y otro para la app; ngrok permite un solo agente por dominio, y gratis ofrece un dominio estático por cuenta → dos cuentas = dos authtokens (`NGROK_AUTHTOKEN_API` / `NGROK_AUTHTOKEN_APP`).
3. **Estado actual del flujo:** el webhook recibe el `Update` real de Telegram y lo registra en logs; la persistencia (SQL Server + EF Core), la cola (RabbitMQ), el tiempo real (SignalR) y el envío por la Bot API están en las Fases 2–6. SQL Server ya corre en el compose; las variables de RabbitMQ están declaradas en `.env`, pero **el servicio RabbitMQ todavía no está en el compose**.
4. **Registro manual del webhook:** hasta la Fase 5 no hay auto-registro al arrancar; mientras tanto se registra con `setWebhook` (ver §4, paso 5).
5. **Fuera de alcance** (según spec): autenticación/usuarios, mensajes no textuales, diseño responsive y paginación del historial.
6. **Puertos por defecto:** API `5088`, frontend `4200`, SQL Server `1433` (solo para desarrollo local; en producción no se publican).
