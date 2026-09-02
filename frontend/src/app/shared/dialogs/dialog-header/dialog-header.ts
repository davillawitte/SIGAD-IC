import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { PciIconComponent } from '@davillawitte/pci-design-system';
import type { PciIconName } from '@davillawitte/pci-design-system';

/** Selo circular + título centralizado + risco dourado + "X" de fechar — visual padrão de todos
 * os modais do app (referência: `docs/models images/modal choices.png` e `trocar senha.png`).
 * Sem equivalente no design system — ver `docs/design-system-pendencias.md`. */
@Component({
  selector: 'app-dialog-header',
  imports: [CommonModule, PciIconComponent],
  templateUrl: './dialog-header.html',
  styleUrl: './dialog-header.scss',
})
export class AppDialogHeaderComponent {
  @Input({ required: true }) icon!: PciIconName;
  @Input({ required: true }) title!: string;
  /** Some quando não há como fechar por fora do fluxo do formulário (ex.: passo obrigatório). */
  @Input() closable = true;
  @Output() closed = new EventEmitter<void>();
}
