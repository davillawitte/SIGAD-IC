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

export type ExportEscalaExcelOpcao = 'resumida' | 'completa';
export type ExportEscalaPdfTipo = 'definitiva' | 'resumida';

/** Um ícone só de "Baixar" pra não repetir o mesmo ícone de download em duas ações inline — o
 * diálogo pergunta o formato (PDF ou Excel) e, dentro de cada um, um sub-formato: PDF pergunta
 * escala definitiva ou escala resumida (mapa de plantão físico por equipe/local — só disponível
 * quando a escala tem uma escala resumida vinculada); Excel pergunta resumida (auxílio-
 * alimentação) ou completa. */
export type ExportEscalaResultado =
  | { formato: 'pdf'; pdfTipo: ExportEscalaPdfTipo }
  | { formato: 'excel'; opcao: ExportEscalaExcelOpcao };

export interface ExportEscalaDialogData {
  title?: string;
  /** Se a escala desta linha tem uma escala resumida vinculada — desabilita a opção "Escala
   * resumida" do PDF quando não tem. */
  temEscalaResumida?: boolean;
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

  readonly formato = signal<'pdf' | 'excel'>('pdf');
  readonly pdfTipo = signal<ExportEscalaPdfTipo>('definitiva');
  readonly excelOpcao = signal<ExportEscalaExcelOpcao>('resumida');

  confirm(): void {
    this.ref.close(
      this.formato() === 'pdf'
        ? { formato: 'pdf', pdfTipo: this.pdfTipo() }
        : { formato: 'excel', opcao: this.excelOpcao() },
    );
  }

  cancel(): void {
    this.ref.close(null);
  }
}
