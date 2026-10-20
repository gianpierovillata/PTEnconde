import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { routes } from './app.routes';
import { provideClientHydration } from '@angular/platform-browser';
import { AppConfigService } from './core/config/app-config.service';
import { ngrokInterceptor } from './core/interceptors/ngrok.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(),
    // ngrokInterceptor evita la página de advertencia de ngrok free (ERR_NGROK_6024).
    provideHttpClient(withInterceptors([ngrokInterceptor])),
    // Carga /assets/config.json antes de renderizar (URLs de API y SignalR en runtime).
    provideAppInitializer(() => inject(AppConfigService).cargar()),
  ]
};
