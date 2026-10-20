# AGENTS.md

Guía para las sesiones de OpenCode en este repositorio. Todo se escribe en **español**: `README.md`, ADRs, diagramas y comentarios de documentación.

## Qué es este repositorio

Prueba técnica para **encodelabs**: una bandeja web ("bandeja conversacional") donde un operador recibe y responde, en tiempo real, los mensajes de texto enviados a un bot de Telegram.

- `Proyecto/chat_inbox_tech_test (5).md` — **especificación autoritativa de requisitos** (alcance, stack, entregables, criterios de evaluación). Leerla antes de tomar decisiones de alcance.
- `EncodeAssistant/` — la solución .NET. El único código real hoy es `encode-api/` (minimal API, .NET 10). `EncodeAssistant.slnx` es una solución **vacía**.
- `Proyecto/PLAN.md` — plan de trabajo vivo (fases, decisiones y checklist de entregables). Actualizarlo al implementar funcionalidades.
- `Proyecto/AnalisisC4/` — carpeta prevista para los diagramas C4 (hoy vacía).
- `Proyecto/Prompts.md` — prompts originales del usuario, no es fuente de requisitos.

El repositorio es un andamiaje. **No** existen todavía ADRs, tests ni CI. No asumir que existan. Sí existen el `.gitignore` raíz, `.env.example`, los Dockerfiles y `docker-compose.yml` (servicios `sqlserver`, `api`, `frontend`, `ngrok-api` y `ngrok-app`).

## Comandos

La solución `EncodeAssistant/EncodeAssistant.slnx` ya referencia `encode-api`, pero se recomienda apuntar al csproj para no depender de ese estado.

```bash
dotnet build EncodeAssistant/encode-api/encode-api.csproj
dotnet run   --project EncodeAssistant/encode-api/encode-api.csproj   # perfil http -> http://localhost:5088
dotnet build EncodeAssistant/EncodeAssistant.slnx                     # opcional (compila el host)
```

- Requiere el **SDK de .NET 10** (verificado con 10.0.402).
- Único endpoint mapeado actualmente: `GET /health`. OpenAPI se expone en Development.
- No hay proyecto de tests todavía. Si se agregan, la spec los limita a **tests unitarios sobre el dominio**.

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

```bash
docker compose up --build            # usa el .env local (gitignored)
docker compose --env-file .env.example config   # validar sin tocar el .env local
```

- En esta máquina, la shell del agente puede no tener cargado el grupo `docker` (`permission denied` en el socket). Encadenar con `sg docker -c "..."` o re-loguear.
- Si se cambia `SQLSERVER_SA_PASSWORD` después del primer arranque, hay que recrear el volumen (`docker compose down -v`): SQL Server solo aplica la contraseña la primera vez.
- `RabbitMQ` todavía no está en el compose (ver Fase 7 de `Proyecto/PLAN.md`).

## Restricciones de arquitectura (de la spec)

- Backend: .NET con **Clean Architecture** (obligatorio y criterio evaluado).
- Stack esperado: RabbitMQ (mensajes entrantes), SignalR (tiempo real), SQL Server (persistencia), ngrok (URL pública del webhook), frontend Angular.
- Todo debe levantarse con un único `docker compose up` después de que el operador complete el `.env`.

## Entregables que faltan crear

`README.md` raíz con las 6 secciones que pide la spec, diagramas C4 Nivel 1 + 2 (SVG o XML de draw.io), ADR-001..003 (base de datos, tecnología de tiempo real, flujo del webhook + topología de RabbitMQ) y el documento de una página "Trabajo con IA".

## Puntos de atención

- El `.env` de la raíz tiene credenciales locales de desarrollo y está ignorado por git: **nunca** forzarlo al repo (`git add -f`). Solo se versiona `.env.example`.
- `docker-compose.yml` ya inyecta las variables de configuración (formato `Seccion__Clave`), pero el código de `encode-api` todavía **no las consume** — el cableado de configuración queda para las próximas fases.
- `EncodeAssistant/encode-api/encode-api.http` documenta los endpoints placeholder (`/api/v1/health`, `/api/v1/webhook/receiveMessage`, `/api/v1/webhook/sendMessage`); se reemplazan por su versión real en las fases 3 y 5.
- Git está inicializado en `main` con 6 commits (`085b43b` … `2c5872c`); `origin` configurado.
