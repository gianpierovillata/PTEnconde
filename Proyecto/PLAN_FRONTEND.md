# Plan de Frontend — encondechatapp (Angular 22)

> **Estado:** Solo plan. **No se crea código aún**. Se seguirá este orden cuando se implemente.

## 1. Objetivo

Crear una **página de inicio** con:
- **Panel lateral**: listado de usuarios/contactos (Telegram).
- **Panel central**: conversación de chat (historial + usuario seleccionado + campo para responder).

Requisitos: Angular 22 (standalone components), CSS puro (sin Tailwind), sin implementar lógica de negocio (solo estructura + stubs de servicios), alineado al estilo de [encodelabs.com.ar](https://encodelabs.com.ar/es/) (colores neutros + acento, interfaz sencilla y prolija).

## 2. Decisiones de arquitectura (frontend)

| Aspecto | Decisión | Justificación |
|---|---|---|
| Componentes | **Standalone** (Angular 22) | Estándar actual, sin NgModules. Coincide con el proyecto generado. |
| Estado | **Signals** (`signal()`, `computed()`) | Moderno, ligero y compatible con zoneless. |
| Estilos | **CSS global + CSS por componente** | Pedido explícito CSS. Usar variables CSS (`:root`) para tokens de diseño. |
| Estructura | Feature-based (`features/inbox/`) + Core (`core/models`, `core/services`, `core/interfaces`) | Escalable, mantiene separación clara (prepara para Clean Architecture en frontend). |
| SSR | **Mantener SSR** (actual `outputMode: server + ssr`) | Proyecto generado con SSR. No eliminarlo (podría afectar build). Docker se adaptará a SSR o a SPA según despliegue. |
| Tipado | **Strict** (heredado de tsconfig) | Buenas prácticas. |

## 3. Estructura de carpetas propuesta

Crear solo la estructura (plan). **No crear archivos aún**.

```text
src/
├── app/
│   ├── core/
│   │   ├── models/
│   │   │   ├── contact.model.ts        (interfaz vacía/stub)
│   │   │   └── message.model.ts        (interfaz vacía/stub)
│   │   ├── services/
│   │   │   ├── contact.service.ts      (stub, sin lógica)
│   │   │   └── chat.service.ts         (stub, sin lógica)
│   │   └── utils/                      (reservado, vacío por ahora)
│   ├── features/
│   │   └── inbox/
│   │       ├── inbox.component.ts
│   │       ├── inbox.component.html
│   │       ├── inbox.component.css
│   │       ├── sidebar/
│   │       │   ├── sidebar.component.ts
│   │       │   ├── sidebar.component.html
│   │       │   └── sidebar.component.css
│   │       └── chat/
│   │           ├── chat.component.ts
│   │           ├── chat.component.html
│   │           ├── chat.component.css
│   │           ├── chat-header/
│   │           │   ├── chat-header.component.ts
│   │           │   ├── chat-header.component.html
│   │           │   └── chat-header.component.css
│   │           ├── message-list/
│   │           │   ├── message-list.component.ts
│   │           │   ├── message-list.component.html
│   │           │   └── message-list.component.css
│   │           └── message-input/
│   │               ├── message-input.component.ts
│   │               ├── message-input.component.html
│   │               └── message-input.component.css
│   ├── shared/                        (reservado para componentes reutilizables)
│   ├── app.routes.ts                  (modificar: ruta '' → inbox)
│   ├── app.config.ts                  (sin cambios por ahora)
│   └── app.html                       (reemplazar placeholder por <router-outlet>)
├── styles.css                        (tokens CSS + resets)
└── environments/                      (crear solo si necesario, plan)
```

> Nota: Los subcomponentes de `chat/` (`chat-header`, `message-list`, `message-input`) **son opcionales pero recomendados** para mantener responsabilidades únicas (SRP). Se incluirán en el plan (estructura lista), sin implementarlos con lógica.

## 4. Componentes — responsabilidades (solo definición)

| Componente | Tipo | Responsabilidad (definición, sin lógica) |
|---|---|---|
| `InboxComponent` | Container (feature) | Layout principal: divide pantalla en **sidebar (izquierda)** y **chat (derecha)**. Recibe/expone estado vía signals (solo esqueleto). |
| `SidebarComponent` | Presentational | Muestra listado de contactos/usuarios. Espera un array de contactos (input signal/stub). Muestra: **nombre/username**, **cantidad de no leídos**, **vista previa del último mensaje**. Sin acciones implementadas (solo markup). |
| `ChatComponent` | Container | Agrupa `ChatHeader`, `MessageList`, `MessageInput`. Gestiona conversación seleccionada (solo estructura). |
| `ChatHeaderComponent` | Presentational | Muestra nombre/username del contacto seleccionado. Espacio para estado (vacío por ahora). |
| `MessageListComponent` | Presentational | Muestra historial de mensajes (burbujas izquierda/derecha). Solo estructura vacía/list placeholder. |
| `MessageInputComponent` | Presentational | Campo de texto + botón "Enviar". Emite evento `send` (stub de salida) sin lógica. |

## 5. Modelos y Servicios (stubs, sin implementar)

### Modelos (`core/models/`)

**`contact.model.ts`** (interfaz, solo definición):
- `id: string | number`
- `name?: string`
- `username?: string`
- `lastMessage?: string`
- `unreadCount: number`
- `avatarUrl?: string`
- `isActive?: boolean`

**`message.model.ts`** (interfaz, solo definición):
- `id: string | number`
- `contactId: string | number`
- `text: string`
- `direction: 'incoming' | 'outgoing'`  (usuario→operador vs operador→usuario)
- `createdAt: Date | string`
- `read?: boolean`

> Solo interfaces/types. **Sin clases, sin lógica, sin mocks con datos reales** (placeholder vacío).

### Servicios (`core/services/`)

**`contact.service.ts`** (stub standalone, sin implementación):
- Métodos definidos (firmas) pero **vacíos/cuerpo sin lógica**: `getContacts()`, `getContact(id)`, `markAsRead(id)`. Retornan `Observable`/`Signal` vacíos (solo tipos). No HTTP, no datos.

**`chat.service.ts`** (stub standalone, sin implementación):
- Firmas: `getMessages(contactId)`, `sendMessage(contactId, text)`, `onNewMessage()` (para SignalR futuro). Solo contratos. **Sin conexión a API aún**.

## 6. Routing

Modificar **únicamente** `app.routes.ts`:
- Ruta por defecto `''` → carga `InboxComponent` (standalone).
- Mantener `app.routes.server.ts` tal cual (SSR routes).
- No añadir rutas extra.

## 7. Estilos (tokens CSS + layout)

### `styles.css` (global)
Definir **tokens de diseño** basados en [encodelabs.com.ar](https://encodelabs.com.ar/es/). Estilo minimalista, sin sombras, sin transiciones, bordes finos y alto contraste.

**Tokens propuestos:**
```css
:root {
  /* Layout */
  --bg-app: #ffffff;
  --bg-sidebar: #ffffff;
  --bg-chat: #ffffff;
  --bg-surface: #ffffff;

  /* Bordes */
  --border-color: #e5e5e5;
  --divider-color: #e5e5e5;
  --input-border: #e5e5e5;

  /* Tipografía */
  --text-primary: #1a1a1a;
  --text-secondary: #6f6f6f;
  --text-tertiary: #8e8e8e;
  --font-sans: system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;

  /* Estados */
  --hover-bg: #f7f7f7;
  --active-bg: #f0f0f0;
  --selected-bg: #f0f0f0;

  /* Badge no leídos */
  --unread-bg: #000000;
  --unread-text: #ffffff;

  /* Mensajes */
  --incoming-bg: #f5f5f5;
  --incoming-text: #1a1a1a;
  --outgoing-bg: #000000;
  --outgoing-text: #ffffff;

  /* Input */
  --input-bg: #ffffff;

  /* Acciones */
  --accent: #000000;
  --accent-text: #ffffff;
}

* { box-sizing: border-box; margin: 0; padding: 0; }

html, body {
  height: 100dvh;
  font-family: var(--font-sans);
  background: var(--bg-app);
  color: var(--text-primary);
  font-size: 14px;
  line-height: 1.5;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}

/* Sin transiciones globales */
*,
*::before,
*::after {
  transition: none !important;
}
```

### Layout (Inbox)
- **CSS Grid**: `grid-template-columns: 320px 1fr` (sidebar fijo, chat flexible). Altura `100dvh`.
- **Sidebar**: `border-right: 1px solid var(--border-color)`, fondo blanco, sin sombra.
- **Chat**: fondo blanco, sin sombra.
- **Sin bordes redondeados**: mantener recto (0). Solo usar `4px` si es estrictamente necesario para elementos muy pequeños.
- **Sin sombras ni transiciones**.
- **Scroll independiente**: `overflow-y: auto` en `.sidebar-list` y `.message-list`.
- **Tipografía minimalista**: usar pesos 400/500/600 según jerarquía. Sin decoraciones innecesarias.

## 8. Orden de implementación (paso a paso, sin crear aún)

Cuando se autorice a crear código, seguir este orden **estrictamente**:

1. **Crear modelos** (`core/models/*`) — solo interfaces.
2. **Crear servicios stubs** (`core/services/*`) — solo firmas, sin lógica.
3. **Crear `InboxComponent`** (layout vacío) + HTML/CSS base.
4. **Crear `SidebarComponent`** + HTML/CSS (estructura de lista, placeholders).
5. **Crear `ChatComponent`** + integrar subcomponentes.
6. **Crear `ChatHeaderComponent`** (estructura).
7. **Crear `MessageListComponent`** (estructura de burbujas).
8. **Crear `MessageInputComponent`** (input + botón).
9. **Actualizar `app.routes.ts`** → ruta raíz a `InboxComponent`.
10. **Actualizar `app.html`** → dejar solo `<router-outlet />` (limpiar placeholder Angular).
11. **Ajustar `styles.css`** con tokens + resets.
12. **Verificar build** (`ng build --configuration development`) sin errores.
13. **Verificar estructura** (compilación TypeScript OK).

## 9. Dockerización (plan para después)

**Objetivo:** añadir `frontend` como servicio en `docker-compose.yml` (multi-stage), servir Angular. Dos opciones viables con SSR actual:

| Opción | Build | Servidor | Uso |
|---|---|---|---|
| **A. SSR (Node/Express)** | `ng build` genera `dist/encondechatapp/browser` + `server/server.mjs` | Ejecutar `node dist/encondechatapp/server/server.mjs` (imagen Node Alpine) | SSR completo (requiere Node en runtime). |
| **B. SPA + Nginx** | Build solo browser (`ng build --configuration production --ssr=false` o usar output estático) | Servir `dist/encondechatapp/browser` con `nginx:alpine` + `try_files $uri $uri/ /index.html` | Más ligero, común para SPA. **Recomendado** para esta app (chat SPA + SignalR). SSR no aporta valor crítico aquí. |

**Recomendación:** **Opción B (SPA + Nginx)**. Mantener SSR en código pero para imagen de producción servir estático (simplifica y reduce superficie). Documentar decisión en ADR si se elige B.

### 9.1 Archivos a crear (plan)

- `EncodeAssistant/encondechatapp/.dockerignore`
  - Ignorar: `node_modules/`, `dist/`, `.angular/`, `.git/`, `*.log`, `.env*`, `coverage/`
- `EncodeAssistant/encondechatapp/Dockerfile` (multi-stage)
  - **Stage 1 (build):** `node:22-alpine` → `WORKDIR /app`, `COPY package*.json ./`, `RUN npm ci --no-audit --no-fund`, `COPY . .`, `RUN npm run build` (producción). Para SPA: `ng build --configuration production`.
  - **Stage 2 (serve):** `nginx:1.27-alpine` → copiar `dist/encondechatapp/browser/*` a `/usr/share/nginx/html/`, añadir `nginx.conf` con fallback SPA, usuario no-root, `EXPOSE 80`.

- `EncodeAssistant/encondechatapp/nginx.conf` (opcional)
  - `try_files $uri $uri/ /index.html`
  - Headers de seguridad básicos
  - Gzip

### 9.2 docker-compose.yml (añadir servicio)

Añadir `frontend`:
```yaml
frontend:
  build:
    context: ./EncodeAssistant/encondechatapp
  image: ${COMPOSE_PROJECT_NAME:-enconde-inbox}-frontend
  environment:
    # Inyectados para que Angular los lea en runtime (si se usa app-config runtime)
    FRONTEND_API_BASE_URL: ${FRONTEND_API_BASE_URL:-http://localhost:5088}
    FRONTEND_SIGNALR_URL: ${FRONTEND_SIGNALR_URL:-http://localhost:5088/hubs/inbox}
  ports:
    - "${FRONTEND_PORT:-4200}:80"
  depends_on:
    - api
  networks:
    - backend
  restart: unless-stopped
```

> Nota: Nginx escucha en 80 interno. Se publica `${FRONTEND_PORT:-4200}:80` (coherente con `.env.example`: `FRONTEND_PORT=4200`).

### 9.3 Consideraciones de seguridad/despliegue
- Usuario **no-root** en nginx (imagen Alpine permite ajustes o usar `nginxinc/nginx-unprivileged`).
- `.dockerignore` obligatorio.
- Variables de entorno: para build-time vs runtime. Angular no lee env en runtime fácilmente con build estático → opción: generar `assets/config.json` en entrypoint o usar `APP_INITIALIZER` + fetch de config (plan para implementación, no ahora).
- Healthcheck opcional (`curl -f http://localhost/ || exit 1`).
- CORS ya gestionado en API (`CORS_ALLOWED_ORIGINS` incluirá origen del frontend).

## 10. Criterios de aceptación (plan cumplido)

- [ ] Estructura de carpetas definida (sin archivos creados aún) — **este plan**.
- [ ] Componentes identificados con responsabilidades claras.
- [ ] Modelos/servicios son **solo stubs** (sin lógica, sin implementación).
- [ ] Layout: panel lateral 320px + panel central flexible, 100dvh.
- [ ] Estilo alineado a encodelabs.com.ar (tokens neutros + profesional).
- [ ] SSR se mantiene (sin romper configuración actual).
- [ ] Dockerización planificada (SPA+Nginx recomendado, multi-stage, no-root).
- [ ] **No se ha escrito código TypeScript/HTML/CSS todavía** (solo este `.md`).

## 11. Próximos pasos

1. **Aprobar este plan** (usuario).
2. **Crear únicamente la estructura + archivos vacíos/stubs** (sin lógica) siguiendo orden §8.
3. **Dockerizar** (Fase 7 ampliada) siguiendo §9.
4. **Conectar API + SignalR** en fases posteriores (no forma parte de este plan).