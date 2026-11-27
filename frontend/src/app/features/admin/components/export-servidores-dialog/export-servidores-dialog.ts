import { Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  PciButtonComponent,
  PciCheckboxComponent,
  PciFormActionsComponent,
  PciFormCardComponent,
  PciIconComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';

import { AppDialogHeaderComponent } from '../../../../shared/dialogs/dialog-header/dialog-header';

export interface ExportServidoresDialogData {
  /** Servidores na listagem atual (com filtros e busca aplicados, todas as páginas). */
  totalListagem: number;
  /** Servidores cadastrados no total. */
  totalGeral: number;
}

export interface ExportServidoresResultado {
  abrangencia: 'listagem' | 'todos';
  agruparPorSetor: boolean;
  formato: 'pdf' | 'excel';
}

/** Exportação da tela Servidores: quais (listagem atual ou todos), separado por setor ou não e
 * o formato. A ordem é sempre alfabética. */
@Component({
  selector: 'app-export-servidores-dialog',
  imports: [
    MatDialogModule,
    PciButtonComponent,
    PciCheckboxComponent,
    PciFormActionsComponent,
    PciFormCardComponent,
    PciIconComponent,
    PciStackComponent,
    AppDialogHeaderComponent,
  ],
  templateUrl: './export-servidores-dialog.html',
  styleUrl: './export-servidores-dialog.scss',
})
export class ExportServidoresDialog {
  readonly data = inject<ExportServidoresDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ExportServidoresDialog, ExportServidoresResultado | null>);

  readonly abrangencia = signal<'listagem' | 'todos'>('listagem');
  readonly agruparPorSetor = signal(false);
  readonly formato = signal<'pdf' | 'excel'>('pdf');

  confirm(): void {
    this.ref.close({
      abrangencia: this.abrangencia(),
      agruparPorSetor: this.agruparPorSetor(),
      formato: this.formato(),
    });
  }

  cancel(): void {
    this.ref.close(null);
  }
}
