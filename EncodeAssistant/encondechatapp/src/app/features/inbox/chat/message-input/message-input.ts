import { Component, input, output, signal } from '@angular/core';

/** Caja de respuesta del operador. Deshabilitada si no hay conversación abierta. */
@Component({
  imports: [],
  selector: 'app-message-input',
  styleUrl: './message-input.css',
  templateUrl: './message-input.html',
})
export class MessageInput {
  readonly habilitado = input(false);

  readonly enviar = output<string>();

  protected readonly texto = signal('');

  protected actualizarTexto(valor: string): void {
    this.texto.set(valor);
  }

  protected enviarMensaje(): void {
    const contenido = this.texto().trim();
    if (!contenido || !this.habilitado()) {
      return;
    }

    this.enviar.emit(contenido);
    this.texto.set('');
  }

  /** Enter envía; Shift+Enter agrega un salto de línea. */
  protected alPresionarTecla(evento: KeyboardEvent): void {
    if (evento.key === 'Enter' && !evento.shiftKey) {
      evento.preventDefault();
      this.enviarMensaje();
    }
  }
}
