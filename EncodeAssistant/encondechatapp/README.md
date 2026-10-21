# encondechatapp — bandeja conversacional (frontend)

App Angular 22 con SSR que muestra la bandeja del operador: sidebar de conversaciones de Telegram, historial en burbujas, respuesta en vivo y borrado de conversaciones.

Este proyecto se generó con [Angular CLI](https://github.com/angular/angular-cli) 22.2.3.

## Cómo correrlo

### Con Docker (recomendado, es lo que usa la solución completa)

Desde la raíz del repositorio:

```bash
docker compose up --build frontend
```

El contenedor hace el build y lo sirve con nginx en `http://localhost:4200`. El resto de los servicios (API, SQL Server, ngrok) se levantan con `docker compose up --build`.

### En desarrollo local

```bash
npm install
npm start        # ng serve en http://localhost:4200
```

Necesita la API corriendo (`http://localhost:5088`). En desarrollo la app lee `public/assets/config.json`, que ya apunta a ese puerto.

## Configuración en runtime

La app **no** tiene las URLs de la API compiladas: las lee de `/assets/config.json` al arrancar (`AppConfigService`). En el contenedor ese archivo lo genera `docker-entrypoint.sh` a partir de las variables de entorno:

| Variable | Contenido |
|---|---|
| `FRONTEND_API_BASE_URL` | URL base de la API (ej. `http://localhost:5088`) |
| `FRONTEND_SIGNALR_URL` | URL del hub (ej. `http://localhost:5088/hubs/inbox`) |

En desarrollo se usa `public/assets/config.json` con valores locales. Así el mismo build sirve para local y para la URL pública de ngrok, sin recompilar.

## Estructura

```
src/app/
  core/
    config/app-config.service.ts          lee /assets/config.json en runtime
    models/                               Contact, Message
    services/                             ContactService, ChatService, RealtimeService
    interceptors/ngrok.interceptor.ts     header ngrok-skip-browser-warning
  features/inbox/
    inbox.ts / inbox.html                 estado de la bandeja (+ diálogo de confirmación)
    sidebar/                              lista de conversaciones y botones de borrado
    chat/                                 cabecera, historial y caja de respuesta
  shared/confirm-dialog/                  diálogo modal reutilizable
```

## Notas que ahorran tiempo

- **SSR/prerender:** todas las rutas se prerenderizan. Los datos se cargan en `afterNextRender` (solo navegador), no en `ngOnInit`; con un guard de plataforma la app se quedaba con el HTML del servidor y el sidebar vacío.
- **API y SignalR no pasan por el mismo camino.** el interceptor HTTP no cubre SignalR: el header `ngrok-skip-browser-warning` se agrega por separado en el `withUrl` de `RealtimeService`.
- **Después de cambiar código hay que rehacer el build** (`npm run build`) y recrear el contenedor del frontend; nginx sirve el bundle ya compilado, no hay hot reload en Docker. Después, hard refresh en el navegador.
- El componente que consulta la lista de contactos está en `Inbox`; el sidebar y el chat son presentacionales (reciben `input` y emiten `output`).

## Build de producción

```bash
npm run build
```

Los artefactos quedan en `dist/encondechatapp` (browser + server para SSR). El `Dockerfile` hace exactamente esto en una etapa y los copia a nginx.

## Tests

Hay soporte de Vitest (`ng test`), pero el proyecto **no tiene tests propios todavía**; la spec de la prueba técnica solo admite tests unitarios sobre el dominio del backend.
