import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { PLATFORM_ID } from '@angular/core';
import { Subject, takeUntil } from 'rxjs';
import { Sidebar } from './sidebar/sidebar';
import { Chat } from './chat/chat';
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
  imports: [Sidebar, Chat],
  selector: 'app-inbox',
  styleUrl: './inbox.css',
  templateUrl: './inbox.html',
})
export class Inbox implements OnInit, OnDestroy {
  private readonly platformId = inject(PLATFORM_ID);
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

  /** true mientras hay una conversación abierta (habilita el input de respuesta). */
  readonly puedeResponder = computed(() => this.contactoSeleccionado() !== null);

  ngOnInit(): void {
    // En SSR no hay API ni SignalR: la bandeja se hidrata y carga en el navegador.
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    this.cargarContactos();
    this.suscribirTiempoReal();
    void this.realtime.conectar();
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
