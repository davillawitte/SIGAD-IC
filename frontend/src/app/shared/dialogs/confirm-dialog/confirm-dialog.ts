import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  PciButtonComponent,
  PciFormActionsComponent,
  PciIconComponent,
} from '@davillawitte/pci-design-system';
import type { PciIconName } from '@davillawitte/pci-design-system';

import { AppDialogHeaderComponent } from '../dialog-header/dialog-header';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
  /** Ícone do selo — por padrão `alert` (danger) ou `help-circle`; sobrescrever pra casos
   * específicos (ex.: `user-plus` em "Cadastrar usuário?"). */
  icon?: PciIconName;
  /** Rótulo + ação de um link opcional abaixo da mensagem (ex.: "Clique aqui para visualizar" um
   * detalhamento em outro modal) — sem componente de link no design system, ver
   * `.pci-app-dialog__link` em styles.scss. */
  viewDetailsLabel?: string;
  onViewDetails?: () => void;
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [
    CommonModule,
    MatDialogModule,
    PciButtonComponent,
    PciFormActionsComponent,
    PciIconComponent,
    AppDialogHeaderComponent,
  ],
  templateUrl: './confirm-dialog.html',
})
export class ConfirmDialog {
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ConfirmDialog, boolean>);

  confirm(): void {
    this.ref.close(true);
  }

  cancel(): void {
    this.ref.close(false);
  }
}
