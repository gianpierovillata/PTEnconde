import { Injectable, NgZone, inject, isDevMode } from '@angular/core';
import { Subject } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AppConfigService } from '../config/app-config.service';
import { Contact } from '../models/contact.model';
import { Message } from '../models/message.model';

/**
 * Conexión SignalR con la API (hub `/hubs/inbox`).
 * Reemite los eventos del servidor:
 *  - `NuevoMensaje`       → hay un mensaje entrante o una respuesta enviada.
 *  - `ContactoActualizado` → cambió la vista previa o el contador de no leídos.
 */
@Injectable({ providedIn: 'root' })
export class RealtimeService {
  private readonly config = inject(AppConfigService);
  private readonly zone = inject(NgZone);

  private readonly nuevosMensajes$ = new Subject<Message>();
  private readonly contactos$ = new Subject<Contact>();

  private conexion: signalR.HubConnection | null = null;

  /** Emite cada mensaje nuevo (entrante o saliente). */
  readonly nuevoMensaje$ = this.nuevosMensajes$.asObservable();

  /** Emite el contacto cuando cambia su último mensaje o sus no leídos. */
  readonly contactoActualizado$ = this.contactos$.asObservable();

  /** Arranca la conexión (idempotente). Se llama al entrar a la bandeja. */
  async conectar(): Promise<void> {
    if (this.conexion) {
      return;
    }

    const hub = new signalR.HubConnectionBuilder()
      .withUrl(this.config.signalRUrl, {
        // Mismo caso que el interceptor HTTP: sin esto ngrok free responde
        // su página de advertencia (ERR_NGROK_6024) al negotiate desde el navegador.
        headers: { 'ngrok-skip-browser-warning': 'true' },
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(isDevMode() ? signalR.LogLevel.Warning : signalR.LogLevel.Error)
      .build();

    this.conexion = hub;

    // Los callbacks de SignalR corren fuera de Angular: se reentra en la zona
    // para que los signals actualicen la vista.
    hub.on('NuevoMensaje', (mensaje: Message) => {
      this.zone.run(() => this.nuevosMensajes$.next(mensaje));
    });

    hub.on('ContactoActualizado', (contacto: Contact) => {
      this.zone.run(() => this.contactos$.next(contacto));
    });

    hub.onreconnected(() => console.info('[Realtime] Reconectado al hub de la bandeja.'));
    hub.onclose((error) => {
      if (error && isDevMode()) {
        console.warn('[Realtime] Conexión cerrada:', error);
      }
    });

    try {
      await hub.start();
      console.info('[Realtime] Conectado a', this.config.signalRUrl);
    } catch (error) {
      // Sin tiempo real la bandeja sigue usable (refresco manual), por eso no se propaga.
      console.error('[Realtime] No se pudo conectar al hub:', error);
      this.conexion = null;
    }
  }

  /** Corta la conexión al salir de la bandeja. */
  async desconectar(): Promise<void> {
    const hub = this.conexion;
    this.conexion = null;

    if (hub) {
      await hub.stop();
    }
  }
}
