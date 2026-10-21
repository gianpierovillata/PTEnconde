# AGENTS.md

Guía para las sesiones de OpenCode en este repositorio. Todo se escribe en **español**: `README.md`, ADRs, diagramas y comentarios de documentación.

## Qué es este repositorio

Prueba técnica para **encodelabs**: una bandeja web ("bandeja conversacional") donde un operador recibe y responde, en tiempo real, los mensajes de texto enviados a un bot de Telegram.

- `Proyecto/chat_inbox_tech_test (5).md` — **especificación autoritativa de requisitos** (alcance, stack, entregables, criterios de evaluación). Leerla antes de tomar decisiones de alcance.
- `EncodeAssistant/` — la solución .NET. El código real está en `encode-api/` (minimal API, .NET 10). `EncodeAssistant.slnx` **solo referencia `encode-api`** (todavía no hay proyectos de capas).
- `EncodeAssistant/encondechatapp/` — el frontend Angular 22 (bandeja funcional, con SSR).
- `Proyecto/PLAN.md` — plan de trabajo vivo (fases, decisiones y checklist de entregables). Actualizarlo al implementar funcionalidades.
- `Proyecto/AnalisisC4/` — carpeta prevista para los diagramas C4.
- `Proyecto/Prompts.md` — prompts originales del usuario, no es fuente de requisitos.

**No** existen todavía ADRs, tests ni CI. No asumir que existan. Sí existen el `.gitignore` raíz, `.env.example`, los Dockerfiles y `docker-compose.yml` (servicios `sqlserver`, `api`, `frontend`, `ngrok-api` y `ngrok-app`).

La bandeja ya es funcional de punta a punta: webhook → SQL Server → SignalR → Angular, y las respuestas del operador viajan al usuario por la Bot API de Telegram. Lo que falta está en `Proyecto/PLAN.md` (RabbitMQ, separación en capas, registro automático del webhook, entregables).

## Estructura de `encode-api`

No hay capas separadas todavía (todo vive en el host, Fase 1 pendiente):

```
Program.cs                        todos los endpoints, CORS, SignalR, EnsureCreated
Domain/Entities/                  Contacto, Mensaje, Usuario, Operador
Aplication/Services/              BandejaService (casos de uso), MensajeService, UsuarioService
Aplication/Services/TelegramBotClient.cs  cliente de la Bot API (sendMessage, estado de entrega)
Persistence/EncodeDbContext.cs    tablas, índices y borrado en cascada
Hubs/InboxHub.cs                  hub de SignalR (/hubs/inbox)
```

> Ojo con el nombre: la carpeta se llama **`Aplication`** (una sola `c`) y así está el namespace. No "corregirlo" sin renombrar namespaces y `using` en todos lados (está previsto en la Fase 1).

## Comandos

```bash
dotnet build EncodeAssistant/encode-api/encode-api.csproj
dotnet run   --project EncodeAssistant/encode-api/encode-api.csproj   # perfil http -> http://localhost:5088
dotnet build EncodeAssistant/EncodeAssistant.slnx                     # opcional (hoy solo contiene encode-api)

cd EncodeAssistant/encondechatapp
npm install
npm run build     # build de producción (incluye SSR/prerender)
npm start         # dev server en http://localhost:4200
```

- Requiere el **SDK de .NET 10** (verificado con 10.0.402) y **Node.js 22+** para el frontend.
- Al correr la API fuera de Docker hay que darle `ConnectionStrings__SqlServer` (la misma cadena que usa el compose).
- No hay proyecto de tests todavía. Si se agregan, la spec los limita a **tests unitarios sobre el dominio**.
- **No** instalar `Microsoft.EntityFrameworkCore.Design` ni usar `dotnet-ef`: el esquema se crea con `EnsureCreated()`. Esa dependencia arrastraba `Azure.Identity` a la imagen.

### Endpoints actuales (todos en `Program.cs`)

| Método | Ruta |
|---|---|
| `GET` | `/`, `/api/v1/health` |
| `POST` | `/api/telegram/webhook` |
| `GET` | `/api/v1/contacts` |
| `DELETE` | `/api/v1/contacts` |
| `GET` / `DELETE` | `/api/v1/contacts/{chatId}` |
| `GET` / `POST` | `/api/v1/contacts/{chatId}/messages` |
| `POST` | `/api/v1/contacts/{chatId}/read` |
| `DELETE` | `/api/v1/messages/{id}` |
| SignalR | `/hubs/inbox` (`NuevoMensaje`, `ContactoActualizado`, `ConversacionEliminada`) |

`EncodeAssistant/encode-api/encode-api.http` mantiene una colección equivalente para VS Code / Rider.

## Docker

`docker-compose.yml` levanta `sqlserver` + `api` + `frontend` + `ngrok-api` + `ngrok-app`. La API escucha en el contenedor en el puerto 8080 y se publica en `${API_HTTP_PORT:-5088}`; SQL Server en `1433`; el frontend (nginx) en `${FRONTEND_PORT:-4200}`.

**ngrok (20/10/2026)** — dos túneles públicos, cada uno con su propia cuenta y URL estática:

| servicio | URL pública | destino | token (solo en `.env`) |
|---|---|---|---|
| `ngrok-api` | `https://${NGROK_DOMAIN_API}` | `host.docker.internal:${API_HTTP_PORT}` (5088) | `NGROK_AUTHTOKEN_API` |
| `ngrok-app` | `https://${NGROK_DOMAIN_APP}` | `host.docker.internal:${FRONTEND_PORT}` (4200) | `NGROK_AUTHTOKEN_APP` |

- Los dos authtoken fueron **verificados** contra el agente de ngrok y las URLs públicas responden (`/api/v1/health` 200 y `/` 200 del frontend).
- `NGROK_DOMAIN_*` se guarda **sin esquema ni barra final**: el agente recibe `--url=https://<dominio>`.
- `extra_hosts: host.docker.internal:host-gateway` es lo que permite salir hacia el host en Linux; si querés salir por la red interna, cambiá el destino a `api:8080` / `frontend:80`.
- Ojo: un ngrok local (binario fuera de Docker) con el mismo dominio bloquea al servicio (`ERR_NGROK_334`). Solo puede haber un agente por dominio.

**Aviso de ngrok free (`ERR_NGROK_6024`).** ngrok intercepta las peticiones de un **navegador** y devuelve su página de advertencia en vez de la respuesta de la API. Como `FRONTEND_API_BASE_URL` apunta al túnel, sin el header `ngrok-skip-browser-warning` el sidebar queda vacío y el navegador reporta un falso error de CORS (`curl` funciona porque no lo intercepta). El frontend lo aplica en `core/interceptors/ngrok.interceptor.ts` y en el `withUrl` de SignalR. Si algo vuelve a "fallar por CORS" en el navegador, es esto.

```bash
docker compose up --build            # usa el .env local (gitignored)
docker compose --env-file .env.example config   # validar sin tocar el .env local
```

- En esta máquina, la shell del agente puede no tener cargado el grupo `docker` (`permission denied` en el socket). Encadenar con `sg docker -c "..."` o re-loguear.
- Si se cambia `SQLSERVER_SA_PASSWORD` después del primer arranque, hay que recrear el volumen (`docker compose down -v`): SQL Server solo aplica la contraseña la primera vez.
- La base se llama **`EncodeDb`** (`SQLSERVER_DB`) y se crea con `EnsureCreated()`: un cambio de modelo **no** se aplica sobre una base existente, hay que recrear el volumen.
- La API recibe `TELEGRAM_BOT_TOKEN` y `TELEGRAM_API_BASE_URL` desde el compose para **enviar las respuestas del operador** (Bot API). Sin token, el envío falla pero el mensaje igual se persiste con `delivered: false`.
- El contenedor de SQL Server **no** trae `sqlcmd`. Para consultar, usar `mcr.microsoft.com/mssql-tools` con `--network enconde-inbox_backend`.
- `RabbitMQ` todavía no está en el compose (ver Fase 7 de `Proyecto/PLAN.md`).

## Frontend: detalles que se pisan fácil

- **Hidratación:** la app usa SSR con `RenderMode.Prerender` para todas las rutas. `Inbox` carga en `afterNextRender`, **no** en `ngOnInit` (con un guard de `isPlatformBrowser`, el navegador se quedaba con el HTML prerenderizado y el sidebar vacío). `AppConfigService.cargar()` también espera a `afterNextRender`.
- **Config en runtime:** `/assets/config.json` lo genera `docker-entrypoint.sh` al arrancar el contenedor desde `FRONTEND_API_BASE_URL` / `FRONTEND_SIGNALR_URL`. En desarrollo se usa `public/assets/config.json` (valores locales).
- **Al tocar el frontend hay que rehacer el build** (`npm run build`) y recrear el contenedor (`docker compose up -d --build frontend`); nginx sirve el bundle ya compilado. Después, hard refresh en el navegador (el nombre del bundle cambia).
- **Verificación sin navegador a mano:** hay Chrome headless disponible. `google-chrome --headless=new --dump-dom` sirve para ver el DOM renderizado, y `--enable-logging=stderr --v=0` para leer la consola real (ahí se ven los errores de hidratación y de red).

## Restricciones de arquitectura (de la spec)

- Backend: .NET con **Clean Architecture** (obligatorio y criterio evaluado).
- Stack esperado: RabbitMQ (mensajes entrantes), SignalR (tiempo real), SQL Server (persistencia), ngrok (URL pública del webhook), frontend Angular.
- Todo debe levantarse con un único `docker compose up` después de que el operador complete el `.env`.

## Entregables que faltan crear

Diagramas C4 (Nivel 1 + 2) en `Proyecto/AnalisisC4/`, ADR-001..003 (base de datos, tecnología de tiempo real, flujo del webhook + topología de RabbitMQ) y el documento de una página "Trabajo con IA". El `README.md` raíz ya tiene las secciones que pide la spec.

## Puntos de atención

- El `.env` de la raíz tiene credenciales locales de desarrollo y está ignorado por git: **nunca** forzarlo al repo (`git add -f`). Solo se versiona `.env.example`.
- `.env.example` usa dominios de ngrok y contraseñas **falsos** a propósito; mantenerlo así.
- Git está inicializado en `main` con 6 commits (`085b43b` … `2c5872c`) e `origin` configurado. Buena parte del trabajo de la bandeja está **sin commitear**.
