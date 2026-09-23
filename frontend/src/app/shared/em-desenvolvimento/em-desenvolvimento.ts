import { Component, input } from '@angular/core';
import { PciAlertComponent, PciPageHeaderComponent } from '@davillawitte/pci-design-system';

/**
 * Página ainda não implementada: cabeçalho normal da tela mais um aviso padronizado, pra quem
 * abrir o item no menu saber que a funcionalidade está a caminho em vez de achar que quebrou.
 */
@Component({
  selector: 'app-em-desenvolvimento',
  imports: [PciAlertComponent, PciPageHeaderComponent],
  templateUrl: './em-desenvolvimento.html',
  styleUrl: './em-desenvolvimento.scss',
})
export class EmDesenvolvimento {
  readonly titulo = input.required<string>();
  readonly descricao = input('');
  /** Complemento do aviso: o que a tela vai oferecer quando ficar pronta. */
  readonly previsao = input('');
}
