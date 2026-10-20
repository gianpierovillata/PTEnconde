import { HttpInterceptorFn } from '@angular/common/http';

/**
 * ngrok free intercepta las peticiones que vienen de un navegador y devuelve su
 * página de advertencia (ERR_NGROK_6024) en vez de la respuesta de la API.
 * El header `ngrok-skip-browser-warning` (con cualquier valor) lo evita.
 *
 * Es inocuo fuera de ngrok: los servidores ignoran headers desconocidos.
 */
export const ngrokInterceptor: HttpInterceptorFn = (peticion, siguiente) => {
  if (peticion.url.includes('ngrok')) {
    return siguiente(peticion.clone({ setHeaders: { 'ngrok-skip-browser-warning': 'true' } }));
  }

  return siguiente(peticion);
};
