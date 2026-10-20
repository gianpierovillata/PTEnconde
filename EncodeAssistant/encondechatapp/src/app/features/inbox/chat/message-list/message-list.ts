import { Component, effect, input, viewChild, ElementRef } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Message } from '../../../../core/models/message.model';

/** Historial de mensajes de la conversación abierta, con autoscroll al último. */
@Component({
  imports: [DatePipe],
  selector: 'app-message-list',
  styleUrl: './message-list.css',
  templateUrl: './message-list.html',
})
export class MessageList {
  readonly mensajes = input<Message[]>([]);
  readonly cargando = input(false);
  readonly hayContacto = input(false);

  private readonly contenedor = viewChild<ElementRef<HTMLElement>>('contenedor');

  constructor() {
    // Al cambiar la conversación o llegar un mensaje, se baja al final.
    effect(() => {
      this.mensajes();
      const elemento = this.contenedor()?.nativeElement;
      if (!elemento) {
        return;
      }

      // queueMicrotask: espera a que Angular pinte los mensajes nuevos.
      queueMicrotask(() => {
        elemento.scrollTop = elemento.scrollHeight;
      });
    });
  }
}
