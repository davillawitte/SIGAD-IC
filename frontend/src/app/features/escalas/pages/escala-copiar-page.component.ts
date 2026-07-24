import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import {
  PciAlertComponent,
  PciFormPageComponent,
  PciInputComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { AppFormColDirective, AppFormSectionComponent } from '../../../shared/form-layout';
import { ESCALAS_ROUTE_PAGES } from '../escalas-route-pages';
import { EscalasApiService } from '../services/escalas-api.service';
import type { EscalaDetail } from '../models/escalas.models';

@Component({
  selector: 'app-escala-copiar-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSelectModule,
    PciAlertComponent,
    PciFormPageComponent,
    PciInputComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './escala-copiar-page.component.html',
})
export class EscalaCopiarPageComponent implements OnInit {
  private readonly api = inject(EscalasApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly routePages = ESCALAS_ROUTE_PAGES;
  readonly origem = signal<EscalaDetail | null>(null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    mes: [1, [Validators.required, Validators.min(1), Validators.max(12)]],
    ano: [2026, [Validators.required, Validators.min(2000), Validators.max(2100)]],
  });

  readonly mesOptions: PciSelectOption[] = [
    { label: 'Janeiro', value: '1' },
    { label: 'Fevereiro', value: '2' },
    { label: 'Março', value: '3' },
    { label: 'Abril', value: '4' },
    { label: 'Maio', value: '5' },
    { label: 'Junho', value: '6' },
    { label: 'Julho', value: '7' },
    { label: 'Agosto', value: '8' },
    { label: 'Setembro', value: '9' },
    { label: 'Outubro', value: '10' },
    { label: 'Novembro', value: '11' },
    { label: 'Dezembro', value: '12' },
  ];

  private origemId = '';

  ngOnInit(): void {
    this.origemId = this.route.snapshot.paramMap.get('id') ?? '';
    this.api.get(this.origemId).subscribe({
      next: (escala) => {
        this.origem.set(escala);
        const next = new Date(escala.ano, escala.mes, 1); // mes is 1-12; Date month is 0-11 so this is next month
        this.form.patchValue({
          mes: next.getMonth() + 1,
          ano: next.getFullYear(),
        });
      },
      error: () => this.error.set('Não foi possível carregar a escala de origem.'),
    });
  }

  save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Informe o mês e o ano de destino.');
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.api
      .copiar(this.origemId, {
        mes: Number(value.mes),
        ano: Number(value.ano),
      })
      .subscribe({
        next: (escala) => void this.router.navigateByUrl(`/escalas/${escala.id}/editar`),
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível copiar a escala.');
          this.saving.set(false);
        },
      });
  }

  cancel(): void {
    void this.router.navigateByUrl(`/escalas/${this.origemId}`);
  }
}
