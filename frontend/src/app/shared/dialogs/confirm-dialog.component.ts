import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  PciButtonComponent,
  PciFormActionsComponent,
  PciFormCardComponent,
} from '@davillawitte/pci-design-system';

export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  danger?: boolean;
}

@Component({
  selector: 'app-confirm-dialog',
  imports: [
    CommonModule,
    MatDialogModule,
    PciButtonComponent,
    PciFormCardComponent,
    PciFormActionsComponent,
  ],
  template: `
    <div class="pci-app-dialog">
      <h2 class="pci-app-dialog__title">{{ data.title }}</h2>
      <pci-form-card>
        <p class="pci-app-dialog__message">{{ data.message }}</p>
      </pci-form-card>
      <pci-form-actions>
        <pci-button variant="ghost" (clicked)="cancel()">{{ data.cancelLabel || 'Cancelar' }}</pci-button>
        <pci-button
          [variant]="data.danger ? 'danger' : 'primary'"
          (clicked)="confirm()"
        >
          {{ data.confirmLabel || 'Confirmar' }}
        </pci-button>
      </pci-form-actions>
    </div>
  `,
})
export class ConfirmDialogComponent {
  readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<ConfirmDialogComponent, boolean>);

  confirm(): void {
    this.ref.close(true);
  }

  cancel(): void {
    this.ref.close(false);
  }
}
