import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciFormActionsComponent,
  PciFormCardComponent,
  PciInputComponent,
  PciSelectComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { AppFormColDirective, AppFormSectionComponent } from '../../../../shared/form-layout';
import {
  AfastamentoItem,
  AfastamentosApiService,
} from '../../../afastamentos/services/afastamentos-api.service';

export interface AfastamentoDialogData {
  servidorOptions: PciSelectOption[];
  defaultServidorId?: string;
  dataInicioPadrao?: string;
  dataFimPadrao?: string;
}

@Component({
  selector: 'app-afastamento-dialog',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    PciAlertComponent,
    PciButtonComponent,
    PciFormCardComponent,
    PciFormActionsComponent,
    PciInputComponent,
    PciSelectComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './afastamento-dialog.html',
})
export class AfastamentoDialog implements OnInit {
  private readonly api = inject(AfastamentosApiService);
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<AfastamentoDialog, AfastamentoItem | false>);
  readonly data = inject<AfastamentoDialogData>(MAT_DIALOG_DATA);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly tipoOptions: PciSelectOption[] = [
    { label: 'FR — Férias', value: 'FR' },
    { label: 'LM — Licença Médica', value: 'LM' },
    { label: 'LP — Licença Prêmio', value: 'LP' },
    { label: 'LO — Licença Outros', value: 'LO' },
  ];

  readonly form = this.fb.nonNullable.group({
    servidorId: ['', Validators.required],
    dataInicio: ['', Validators.required],
    dataFim: ['', Validators.required],
    tipoOcorrenciaCodigo: ['FR', Validators.required],
    sei: [''],
    observacao: [''],
  });

  ngOnInit(): void {
    if (this.data.defaultServidorId) {
      this.form.controls.servidorId.setValue(this.data.defaultServidorId);
    }
    if (this.data.dataInicioPadrao) {
      this.form.controls.dataInicio.setValue(this.data.dataInicioPadrao.slice(0, 10));
    }
    if (this.data.dataFimPadrao) {
      this.form.controls.dataFim.setValue(this.data.dataFimPadrao.slice(0, 10));
    }
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Preencha os campos obrigatórios.');
      return;
    }
    const value = this.form.getRawValue();
    const inicio = value.dataInicio.slice(0, 10);
    const fim = value.dataFim.slice(0, 10);
    const periodoInicio = this.data.dataInicioPadrao?.slice(0, 10);
    const periodoFim = this.data.dataFimPadrao?.slice(0, 10);
    if (periodoInicio && periodoFim && (fim < periodoInicio || inicio > periodoFim)) {
      this.error.set('O período do afastamento deve intersectar o mês da escala.');
      return;
    }
    if (fim < inicio) {
      this.error.set('Data fim deve ser maior ou igual à data início.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    this.api
      .create({
        servidorId: value.servidorId,
        dataInicio: inicio,
        dataFim: fim,
        tipoOcorrenciaCodigo: value.tipoOcorrenciaCodigo,
        observacao: value.observacao.trim() || null,
        sei: value.sei.trim() || null,
      })
      .subscribe({
        next: (item) => this.dialogRef.close(item),
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível salvar o afastamento.');
          this.saving.set(false);
        },
      });
  }

  cancel(): void {
    this.dialogRef.close(false);
  }
}
