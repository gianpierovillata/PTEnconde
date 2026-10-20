# syntax=docker/dockerfile:1
# =============================================================================
# Frontend Angular 22 — build stage + nginx serve (SPA)
# =============================================================================

# ---- Stage 1: build ----
FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci --no-audit --no-fund

COPY . .
RUN npm run build

# ---- Stage 2: serve ----
FROM nginx:1.27-alpine AS final

COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copiar archivos estáticos del build (browser)
COPY --from=build /app/dist/encondechatapp/browser /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
