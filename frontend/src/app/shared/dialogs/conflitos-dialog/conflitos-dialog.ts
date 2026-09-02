import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { PciButtonComponent, PciFormActionsComponent } from '@davillawitte/pci-design-system';

import { AppDialogHeaderComponent } from '../dialog-header/dialog-header';

export interface EscalaConflitoResumo {
  tipo: string;
  critico: boolean;
  servidorNome?: string | null;
  data?: string | null;
  mensagem: string;
}

export interface ConflitosDialogData {
  itens: EscalaConflitoResumo[];
}

@Component({
  selector: 'app-conflitos-dialog',
  imports: [
    CommonModule,
    MatDialogModule,
    PciButtonComponent,
    PciFormActionsComponent,
    AppDialogHeaderComponent,
  ],
  templateUrl: './conflitos-dialog.html',
})
export class ConflitosDialog {
  readonly data = inject<ConflitosDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ConflitosDialog>);

  fechar(): void {
    this.ref.close();
  }
}
