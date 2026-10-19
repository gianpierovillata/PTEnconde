# AGENTS.md

Guía para las sesiones de OpenCode en este repositorio. Todo se escribe en **español**: `README.md`, ADRs, diagramas y comentarios de documentación.

## Qué es este repositorio

Prueba técnica para **encodelabs**: una bandeja web ("bandeja conversacional") donde un operador recibe y responde, en tiempo real, los mensajes de texto enviados a un bot de Telegram.

- `Proyecto/chat_inbox_tech_test (5).md` — **especificación autoritativa de requisitos** (alcance, stack, entregables, criterios de evaluación). Leerla antes de tomar decisiones de alcance.
- `EncodeAssistant/` — la solución .NET. El único código real hoy es `encode-api/` (minimal API, .NET 10). `EncodeAssistant.slnx` es una solución **vacía**.
- `Proyecto/PLAN.md` — plan de trabajo vivo (fases, decisiones y checklist de entregables). Actualizarlo al implementar funcionalidades.
- `Proyecto/AnalisisC4/` — carpeta prevista para los diagramas C4 (hoy vacía).
- `Proyecto/Prompts.md` — prompts originales del usuario, no es fuente de requisitos.

El repositorio es un andamiaje. **No** existen todavía frontend, `docker-compose.yml`, ADRs, tests ni CI. No asumir que existan. Sí existen el `.gitignore` raíz y `.env.example` (variables con valores falsos).

## Comandos

Compilar/ejecutar **debe apuntar al csproj** — el `.slnx` no referencia ningún proyecto, así que `dotnet build` a nivel de solución no compila nada.

```bash
dotnet build EncodeAssistant/encode-api/encode-api.csproj
dotnet run   --project EncodeAssistant/encode-api/encode-api.csproj   # perfil http -> http://localhost:5088
```

- Requiere el **SDK de .NET 10** (verificado con 10.0.402).
- Único endpoint mapeado actualmente: `GET /health`. OpenAPI se expone en Development.
- No hay proyecto de tests todavía. Si se agregan, la spec los limita a **tests unitarios sobre el dominio**.

## Restricciones de arquitectura (de la spec)

- Backend: .NET con **Clean Architecture** (obligatorio y criterio evaluado).
- Stack esperado: RabbitMQ (mensajes entrantes), SignalR (tiempo real), SQL Server (persistencia), ngrok (URL pública del webhook), frontend Angular.
- Todo debe levantarse con un único `docker compose up` después de que el operador complete el `.env`.

## Entregables que faltan crear

`docker-compose.yml`, `README.md` raíz con las 6 secciones que pide la spec, diagramas C4 Nivel 1 + 2 (SVG o XML de draw.io), ADR-001..003 (base de datos, tecnología de tiempo real, flujo del webhook + topología de RabbitMQ) y el documento de una página "Trabajo con IA".

## Puntos de atención

- El `.env` de la raíz contiene un token real. Ya está cubierto por el `.gitignore` raíz y figura como ignorado: **nunca** forzarlo al repo (`git add -f`). Solo se versiona `.env.example`.
- Las variables del `.env` **todavía no las carga** la API — el cableado de configuración/entorno forma parte del trabajo pendiente de docker-compose.
- `EncodeAssistant/encode-api/encode-api.http` llama a `/weatherforecast/`, que **no está mapeado**; el record `WeatherForecast` en `Program.cs` es código sobrante de la plantilla.
- Git está inicializado en `main` y **sin commits todavía**; todo está sin trackear.
