import { Injectable, PLATFORM_ID, inject, isDevMode, signal, afterNextRender } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Configuración de la app cargada en runtime desde /assets/config.json.
 *
 * En Docker, `docker-entrypoint.sh` genera ese archivo a partir de las variables
 * FRONTEND_API_BASE_URL y FRONTEND_SIGNALR_URL del `.env` (así la misma imagen
 * sirve para local y para los túneles de ngrok). En desarrollo (`ng serve`) se usa
 * el `public/assets/config.json` versionado con los valores locales.
 */
export interface AppConfig {
  apiBaseUrl: string;
  signalRUrl: string;
}

const VALORES_POR_DEFECTO: AppConfig = {
  apiBaseUrl: 'http://localhost:5088',
  signalRUrl: 'http://localhost:5088/hubs/inbox',
};

@Injectable({ providedIn: 'root' })
export class AppConfigService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly state = signal<AppConfig>(VALORES_POR_DEFECTO);

  readonly config = this.state.asReadonly();

  get apiBaseUrl(): string {
    return this.state().apiBaseUrl;
  }

  get signalRUrl(): string {
    return this.state().signalRUrl;
  }

  /**
   * Carga /assets/config.json; si falla o no existe, quedan los valores por defecto.
   * Se espera a hidratar: durante el SSR/prerender no hay config.json ni `fetch` útil,
   * y el render del servidor solo produce el HTML inicial.
   */
  async cargar(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) {
      return; // en SSR no hay config.json servido por nginx
    }

    await new Promise<void>((resolver) => afterNextRender(() => resolver()));

    try {
      const respuesta = await fetch('/assets/config.json', { cache: 'no-store' });
      if (!respuesta.ok) {
        return;
      }

      const externo = (await respuesta.json()) as Partial<AppConfig>;
      this.state.set({
        apiBaseUrl: externo.apiBaseUrl?.trim() || VALORES_POR_DEFECTO.apiBaseUrl,
        signalRUrl: externo.signalRUrl?.trim() || VALORES_POR_DEFECTO.signalRUrl,
      });
    } catch {
      if (isDevMode()) {
        console.warn('[AppConfig] No se pudo leer /assets/config.json; se usan los valores locales.');
      }
    }
  }
}
