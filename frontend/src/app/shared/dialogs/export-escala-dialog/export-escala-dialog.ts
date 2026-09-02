import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  PciButtonComponent,
  PciFormActionsComponent,
  PciFormCardComponent,
  PciIconComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';

import { AppDialogHeaderComponent } from '../dialog-header/dialog-header';

export type ExportEscalaCsvOpcao = 'resumida' | 'completa';

/** Um ícone só de "Baixar" pra não repetir o mesmo ícone de download em duas ações inline — o
 * diálogo pergunta o formato (PDF ou CSV) e, se CSV, o sub-formato (resumida ou completa). */
export type ExportEscalaResultado =
  | { formato: 'pdf' }
  | { formato: 'csv'; opcao: ExportEscalaCsvOpcao };

export interface ExportEscalaDialogData {
  title?: string;
}

@Component({
  selector: 'app-export-escala-dialog',
  imports: [
    CommonModule,
    MatDialogModule,
    PciButtonComponent,
    PciFormCardComponent,
    PciFormActionsComponent,
    PciIconComponent,
    PciStackComponent,
    AppDialogHeaderComponent,
  ],
  templateUrl: './export-escala-dialog.html',
  styleUrl: './export-escala-dialog.scss',
})
export class ExportEscalaDialog {
  readonly data = inject<ExportEscalaDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ExportEscalaDialog, ExportEscalaResultado | null>);

  readonly formato = signal<'pdf' | 'csv'>('pdf');
  readonly csvOpcao = signal<ExportEscalaCsvOpcao>('resumida');

  confirm(): void {
    this.ref.close(
      this.formato() === 'pdf' ? { formato: 'pdf' } : { formato: 'csv', opcao: this.csvOpcao() },
    );
  }

  cancel(): void {
    this.ref.close(null);
  }
}
