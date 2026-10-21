import { Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

/**
 * Diálogo de confirmación para acciones destructivas (borrar conversaciones).
 *
 * Es un `<dialog>` nativo: el navegador se encarga del foco, del fondo modal y de
 * cerrar con Escape. El foco inicial va al botón de cancelar, nunca al de confirmar,
 * para que un Enter reflejo no borre nada.
 */
@Component({
  selector: 'app-confirm-dialog',
  styleUrl: './confirm-dialog.css',
  templateUrl: './confirm-dialog.html',
})
export class ConfirmDialog {
  readonly abierto = input(false);
  readonly titulo = input('¿Confirmás la acción?');
  readonly mensaje = input('');
  readonly detalle = input('');
  readonly textoConfirmar = input('Eliminar');

  /** Emite cuando el operador confirma la acción destructiva. */
  readonly confirmar = output<void>();

  /** Emite cuando cancela (botón, Escape o clic en el fondo). */
  readonly cancelar = output<void>();

  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');
  private readonly botonCancelar = viewChild.required<ElementRef<HTMLButtonElement>>('botonCancelar');

  constructor() {
    // `showModal()`/`close()` son imperativas: no alcanza con el atributo `open`.
    // El effect corre después de renderizar, así que las vistas ya existen.
    effect(() => {
      const deberiaEstarAbierto = this.abierto();
      const elemento = this.dialogo().nativeElement;

      if (deberiaEstarAbierto && !elemento.open) {
        elemento.showModal();
        this.botonCancelar().nativeElement.focus();
      } else if (!deberiaEstarAbierto && elemento.open) {
        elemento.close();
      }
    });
  }

  /** Cierre nativo por Escape: el navegador cierra el diálogo y hay que avisarle al padre. */
  alCerrar(): void {
    if (this.abierto()) {
      this.cancelar.emit();
    }
  }
}
