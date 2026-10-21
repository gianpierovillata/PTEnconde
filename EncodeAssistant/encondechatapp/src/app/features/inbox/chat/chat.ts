import { Component, input, output } from '@angular/core';
import { ChatHeader } from './chat-header/chat-header';
import { MessageList } from './message-list/message-list';
import { MessageInput } from './message-input/message-input';
import { Contact } from '../../../core/models/contact.model';
import { Message } from '../../../core/models/message.model';

/** Panel central: cabecera, historial y caja de respuesta de la conversación abierta. */
@Component({
  imports: [ChatHeader, MessageList, MessageInput],
  selector: 'app-chat',
  styleUrl: './chat.css',
  templateUrl: './chat.html',
})
export class Chat {
  readonly contacto = input<Contact | null>(null);
  readonly mensajes = input<Message[]>([]);
  readonly cargando = input(false);
  readonly puedeResponder = input(false);

  readonly enviar = output<string>();

  /** Emite cuando el operador pide eliminar la conversación abierta. */
  readonly eliminar = output<void>();
}
