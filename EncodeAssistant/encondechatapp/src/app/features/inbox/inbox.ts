import { Component, OnDestroy, afterNextRender, computed, inject, signal } from '@angular/core';
import { PLATFORM_ID } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { Sidebar } from './sidebar/sidebar';
import { Chat } from './chat/chat';
import { ConfirmDialog } from '../../shared/confirm-dialog/confirm-dialog';
import { Contact } from '../../core/models/contact.model';
import { Message } from '../../core/models/message.model';
import { ContactService } from '../../core/services/contact.service';
import { ChatService } from '../../core/services/chat.service';
import { RealtimeService } from '../../core/services/realtime.service';

/**
 * Bandeja conversacional: mantiene el estado (contactos, conversación abierta,
 * mensajes) y lo sincroniza en tiempo real con la API por SignalR.
 */
@Component({
  imports: [Sidebar, Chat, ConfirmDialog],
  selector: 'app-inbox',
  styleUrl: './inbox.css',
  templateUrl: './inbox.html',
})
export class Inbox implements OnDestroy {
  private readonly contactosService = inject(ContactService);
  private readonly chatService = inject(ChatService);
  private readonly realtime = inject(RealtimeService);
  private readonly destruido$ = new Subject<void>();

  /** Contactos del sidebar, ordenados por último mensaje. */
  readonly contactos = signal<Contact[]>([]);

  /** Conversación abierta (null = ninguna). */
  readonly contactoSeleccionado = signal<Contact | null>(null);

  /** Mensajes de la conversación abierta. */
  readonly mensajes = signal<Message[]>([]);

  readonly cargandoContactos = signal(false);
  readonly cargandoMensajes = signal(false);

  /** Conversación esperando confirmación de borrado (null = no hay diálogo abierto). */
  readonly pendienteDeEliminar = signal<Contact | null>(null);

  /** true si el diálogo abierto es el de "vaciar la bandeja completa". */
  readonly vaciadoPendiente = signal(false);

  /** Hay un borrado esperando confirmación. */
  readonly confirmacionAbierta = computed(() => this.pendienteDeEliminar() !== null || this.vaciadoPendiente());

  /** Título del diálogo según la acción pendiente. */
  readonly tituloConfirmacion = computed(() =>
    this.vaciadoPendiente() ? '¿Vaciar la bandeja?' : '¿Eliminar la conversación?',
  );

  /** Texto que explica el alcance del borrado. */
  readonly mensajeConfirmacion = computed(() =>
    this.vaciadoPendiente()
      ? `Se eliminan las ${this.contactos().length} conversaciones y todo su historial. Esta acción no se puede deshacer.`
      : 'Se elimina el contacto y todo su historial. Esta acción no se puede deshacer.',
  );

  /** Nombre visible de la conversación que se va a borrar (vacío al vaciar la bandeja). */
  readonly detalleConfirmacion = computed(() => {
    const contacto = this.pendienteDeEliminar();
    if (!contacto) {
      return '';
    }

    return contacto.name?.trim() || (contacto.username ? `@${contacto.username}` : `Chat ${contacto.id}`);
  });

  /** El botón de confirmar cambia de texto según la acción. */
  readonly textoConfirmacion = computed(() =>
    this.vaciadoPendiente() ? `Vaciar (${this.contactos().length})` : 'Eliminar',
  );

  /** true mientras hay una conversación abierta (habilita el input de respuesta). */
  readonly puedeResponder = computed(() => this.contactoSeleccionado() !== null);

  constructor() {
    // afterNextRender solo corre en el navegador (nunca en SSR/prerender), que es
    // donde existen la API y SignalR. Con ngOnInit el componente se quedaba con el
    // HTML prerenderizado, porque el guard de plataforma cortaba antes de cargar.
    afterNextRender(() => {
      this.cargarContactos();
      this.suscribirTiempoReal();
      void this.realtime.conectar();
    });
  }

  ngOnDestroy(): void {
    this.destruido$.next();
    this.destruido$.complete();
    void this.realtime.desconectar();
  }

  /** Abre una conversación: carga el historial y marca los mensajes como leídos. */
  seleccionarContacto(contacto: Contact): void {
    if (String(this.contactoSeleccionado()?.id) === String(contacto.id)) {
      return;
    }

    this.contactoSeleccionado.set(contacto);
    this.mensajes.set([]);
    this.cargandoMensajes.set(true);

    this.chatService
      .getMessages(contacto.id)
      .pipe(takeUntil(this.destruido$))
      .subscribe({
        next: (mensajes) => {
          this.mensajes.set(mensajes);
          this.cargandoMensajes.set(false);
        },
        error: (error) => {
          console.error('[Bandeja] No se pudo cargar la conversación:', error);
          this.cargandoMensajes.set(false);
        },
      });

    this.contactosService
      .markAsRead(contacto.id)
      .pipe(takeUntil(this.destruido$))
      .subscribe({
        next: (actualizado) => this.actualizarContacto(actualizado),
        error: (error) => console.error('[Bandeja] No se pudo marcar como leído:', error),
      });
  }

  /** Envía la respuesta del operador (se persiste en SQL Server y vuelve por SignalR). */
  enviarMensaje(texto: string): void {
    const contacto = this.contactoSeleccionado();
    if (!contacto) {
      return;
    }

    this.chatService
      .sendMessage(contacto.id, texto)
      .pipe(takeUntil(this.destruido$))
      .subscribe({
        next: (mensaje) => this.agregarMensajeSiEsNuevo(mensaje),
        error: (error) => console.error('[Bandeja] No se pudo enviar el mensaje:', error),
      });
  }

  /** Pide confirmación antes de eliminar una conversación (contacto + mensajes). */
  eliminarConversacion(contacto: Contact): void {
    this.pendienteDeEliminar.set(contacto);
  }

  /** Elimina la conversación abierta (botón de la cabecera del chat). */
  eliminarConversacionSeleccionada(): void {
    const contacto = this.contactoSeleccionado();
    if (contacto) {
      this.eliminarConversacion(contacto);
    }
  }

  /** Pide confirmación antes de vaciar la bandeja completa. */
  eliminarTodasLasConversaciones(): void {
    this.vaciadoPendiente.set(true);
  }

  /** Cierra el diálogo sin borrar nada. */
  cancelarEliminacion(): void {
    this.pendienteDeEliminar.set(null);
    this.vaciadoPendiente.set(false);
  }

  /** El operador confirmó: ejecuta el borrado que estaba pendiente. */
  confirmarEliminacion(): void {
    const contacto = this.pendienteDeEliminar();
    this.cancelarEliminacion();

    if (contacto) {
      this.borrarConversacion(contacto);
    } else {
      this.borrarTodo();
    }
  }

  private borrarConversacion(contacto: Contact): void {
    this.contactosService
      .deleteContact(contacto.id)
      .pipe(takeUntil(this.destruido$))
      .subscribe({
        // El evento de SignalR también llega, pero se limpia acá para que la UI
        // reaccione al instante aunque la conexión esté caída.
        next: () => this.quitarContacto(contacto.id),
        error: (error) => console.error('[Bandeja] No se pudo eliminar la conversación:', error),
      });
  }

  private borrarTodo(): void {
    this.contactosService
      .deleteAllContacts()
      .pipe(takeUntil(this.destruido$))
      .subscribe({
        next: () => this.limpiarBandeja(),
        error: (error) => console.error('[Bandeja] No se pudo vaciar la bandeja:', error),
      });
  }

  /** Quita un contacto de la lista y cierra la conversación si era la abierta. */
  private quitarContacto(contactoId: string | number): void {
    this.contactos.update((actuales) => actuales.filter((c) => String(c.id) !== String(contactoId)));

    if (String(this.contactoSeleccionado()?.id) === String(contactoId)) {
      this.contactoSeleccionado.set(null);
      this.mensajes.set([]);
    }
  }

  /** Deja la bandeja vacía (usado al eliminar todas las conversaciones). */
  private limpiarBandeja(): void {
    this.contactos.set([]);
    this.contactoSeleccionado.set(null);
    this.mensajes.set([]);
  }

  private cargarContactos(): void {
    this.cargandoContactos.set(true);

    this.contactosService
      .getContacts()
      .pipe(takeUntil(this.destruido$))
      .subscribe({
        next: (contactos) => {
          this.contactos.set(contactos);
          this.cargandoContactos.set(false);
        },
        error: (error) => {
          console.error('[Bandeja] No se pudo cargar la lista de contactos:', error);
          this.cargandoContactos.set(false);
        },
      });
  }

  private suscribirTiempoReal(): void {
    // Mensaje nuevo (entrante de Telegram o respuesta de otra pestaña).
    this.realtime.nuevoMensaje$
      .pipe(takeUntil(this.destruido$))
      .subscribe((mensaje) => {
        if (String(mensaje.contactId) === String(this.contactoSeleccionado()?.id)) {
          this.agregarMensajeSiEsNuevo(mensaje);

          // La conversación está abierta: se marca leído para que el badge no quede colgado.
          if (mensaje.direction === 'incoming') {
            this.contactosService
              .markAsRead(mensaje.contactId)
              .pipe(takeUntil(this.destruido$))
              .subscribe({ next: (actualizado) => this.actualizarContacto(actualizado) });
          }
        }
      });

    // Vista previa / no leídos / reordenamiento del sidebar.
    this.realtime.contactoActualizado$
      .pipe(takeUntil(this.destruido$))
      .subscribe((contacto) => this.actualizarContacto(contacto));

    // Conversación borrada en otra pestaña (chatId vacío = se vació toda la bandeja).
    this.realtime.conversacionEliminada$
      .pipe(takeUntil(this.destruido$))
      .subscribe((chatId) => (chatId ? this.quitarContacto(chatId) : this.limpiarBandeja()));
  }

  /** Agrega un mensaje al final, sin duplicar los que ya llegaron por SignalR. */
  private agregarMensajeSiEsNuevo(mensaje: Message): void {
    this.mensajes.update((actuales) =>
      actuales.some((m) => String(m.id) === String(mensaje.id)) ? actuales : [...actuales, mensaje],
    );
  }

  /** Inserta o actualiza un contacto y reordena la lista por último mensaje. */
  private actualizarContacto(contacto: Contact): void {
    const estaSeleccionado = String(this.contactoSeleccionado()?.id) === String(contacto.id);

    this.contactos.update((actuales) => {
      const otros = actuales.filter((c) => String(c.id) !== String(contacto.id));
      return [...otros, contacto].sort((a, b) => this.fecha(b.lastMessageAt) - this.fecha(a.lastMessageAt));
    });

    if (estaSeleccionado) {
      this.contactoSeleccionado.set(contacto);
    }
  }

  private fecha(valor: Date | string | undefined): number {
    return valor ? new Date(valor).getTime() : 0;
  }
}
