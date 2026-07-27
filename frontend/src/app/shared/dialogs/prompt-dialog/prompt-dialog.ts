import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciFormActionsComponent,
  PciFormCardComponent,
  PciTextareaComponent,
} from '@davillawitte/pci-design-system';

export interface PromptDialogData {
  title: string;
  label: string;
  placeholder?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  requiredMessage?: string;
}

@Component({
  selector: 'app-prompt-dialog',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    PciAlertComponent,
    PciButtonComponent,
    PciFormCardComponent,
    PciFormActionsComponent,
    PciTextareaComponent,
  ],
  templateUrl: './prompt-dialog.html',
})
export class PromptDialog {
  readonly data = inject<PromptDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<PromptDialog, string | null>);
  private readonly fb = inject(FormBuilder);

  readonly error = signal<string | null>(null);
  readonly form = this.fb.nonNullable.group({
    value: ['', Validators.required],
  });

  confirm(): void {
    this.form.markAllAsTouched();
    const value = this.form.controls.value.value.trim();
    if (!value) {
      this.error.set(this.data.requiredMessage || 'Preencha o campo obrigatório.');
      return;
    }
    this.ref.close(value);
  }

  cancel(): void {
    this.ref.close(null);
  }
}
