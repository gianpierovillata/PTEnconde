import { Component, input } from '@angular/core';
import { Contact } from '../../../../core/models/contact.model';

/** Cabecera de la conversación: usuario de Telegram con el que se está hablando. */
@Component({
  imports: [],
  selector: 'app-chat-header',
  styleUrl: './chat-header.css',
  templateUrl: './chat-header.html',
})
export class ChatHeader {
  readonly contacto = input<Contact | null>(null);

  nombreVisible(): string {
    const contacto = this.contacto();
    if (!contacto) {
      return 'Seleccioná una conversación';
    }

    return contacto.name?.trim() || (contacto.username ? `@${contacto.username}` : `Chat ${contacto.id}`);
  }

  /** Subtítulo: @usuario, o el id de chat si el nombre ya es el @usuario. */
  subtitulo(): string {
    const contacto = this.contacto();
    if (!contacto) {
      return 'Telegram';
    }

    if (contacto.username && contacto.name?.trim()) {
      return `@${contacto.username}`;
    }

    return `Chat ${contacto.id}`;
  }
}
