import { Component, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Contact } from '../../../core/models/contact.model';

/** Lista de conversaciones (sidebar izquierdo de la bandeja). */
@Component({
  imports: [DatePipe],
  selector: 'app-sidebar',
  styleUrl: './sidebar.css',
  templateUrl: './sidebar.html',
})
export class Sidebar {
  /** Contactos ordenados por último mensaje. */
  readonly contactos = input<Contact[]>([]);

  /** Id del contacto abierto (para resaltarlo). */
  readonly seleccionadoId = input<string | number | null>(null);

  readonly cargando = input(false);

  /** Emite el contacto elegido por el operador. */
  readonly seleccionar = output<Contact>();

  /** Emite cuando el operador pide eliminar la conversación de un contacto. */
  readonly eliminar = output<Contact>();

  /** Emite cuando el operador pide vaciar la bandeja completa. */
  readonly eliminarTodo = output<void>();

  estaSeleccionado(contacto: Contact): boolean {
    return this.seleccionadoId() !== null && String(this.seleccionadoId()) === String(contacto.id);
  }

  /** Nombre visible: nombre de Telegram, si no @usuario, si no el chatId. */
  nombreVisible(contacto: Contact): string {
    return contacto.name?.trim() || (contacto.username ? `@${contacto.username}` : `Chat ${contacto.id}`);
  }

  inicial(contacto: Contact): string {
    return (contacto.name?.trim() || contacto.username?.trim() || 'U').charAt(0).toUpperCase();
  }

  /** Hora del último mensaje (HH:mm); vacío si todavía no hay mensajes. */
  horaUltimoMensaje(contacto: Contact): string | null {
    return contacto.lastMessageAt ? new Date(contacto.lastMessageAt).toISOString() : null;
  }
}
