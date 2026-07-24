import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { Router } from '@angular/router';
import {
  PciAlertComponent,
  PciButtonComponent,
  PciFormPageComponent,
  PciInputComponent,
  PciStackComponent,
} from '@davillawitte/pci-design-system';
import type { PciSelectOption } from '@davillawitte/pci-design-system';

import { AuthService } from '../../../core/auth/auth.service';
import { AdminApiService } from '../../admin/services/admin-api.service';
import { AppFormColDirective, AppFormSectionComponent } from '../../../shared/form-layout';
import { ESCALAS_ROUTE_PAGES } from '../escalas-route-pages';
import { EscalasApiService } from '../services/escalas-api.service';
import type { EscalaAnteriorInfo, TipoFuncionamento } from '../models/escalas.models';

function nextMonthYear(): { mes: number; ano: number } {
  const now = new Date();
  const next = new Date(now.getFullYear(), now.getMonth() + 1, 1);
  return { mes: next.getMonth() + 1, ano: next.getFullYear() };
}

@Component({
  selector: 'app-escala-form-page',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSelectModule,
    PciAlertComponent,
    PciButtonComponent,
    PciFormPageComponent,
    PciInputComponent,
    PciStackComponent,
    AppFormSectionComponent,
    AppFormColDirective,
  ],
  templateUrl: './escala-form-page.component.html',
})
export class EscalaFormPageComponent implements OnInit {
  private readonly api = inject(EscalasApiService);
  private readonly adminApi = inject(AdminApiService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly routePages = ESCALAS_ROUTE_PAGES;
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly setorOptions = signal<PciSelectOption[]>([]);
  readonly showSetorSelect = computed(() => this.setorOptions().length > 1);
  readonly setorUnicoLabel = signal<string | null>(null);
  readonly escalaAnterior = signal<EscalaAnteriorInfo | null>(null);

  private readonly next = nextMonthYear();

  readonly form = this.fb.nonNullable.group({
    setorId: ['', Validators.required],
    mes: [this.next.mes, [Validators.required, Validators.min(1), Validators.max(12)]],
    ano: [this.next.ano, [Validators.required, Validators.min(2000), Validators.max(2100)]],
    tipoFuncionamento: ['VinteQuatroHoras' as TipoFuncionamento, Validators.required],
    observacao: [''],
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

  readonly tipoOptions: PciSelectOption[] = [
    { label: '24 horas', value: 'VinteQuatroHoras' },
    { label: 'Expediente', value: 'Expediente' },
  ];

  readonly isSuperAdmin = computed(() => this.auth.isSuperAdmin());

  ngOnInit(): void {
    this.adminApi.listMeusSetores().subscribe({
      next: (setores) => {
        this.setorOptions.set(setores.map((s) => ({ label: `${s.sigla} — ${s.nome}`, value: s.id })));
        if (setores.length === 1) {
          this.form.controls.setorId.setValue(setores[0].id);
          this.form.controls.setorId.disable({ emitEvent: false });
          this.setorUnicoLabel.set(`${setores[0].sigla} — ${setores[0].nome}`);
          this.checkAnterior();
        }
      },
      error: () => this.error.set('Não foi possível carregar os setores disponíveis.'),
    });

    this.form.controls.setorId.valueChanges.subscribe(() => this.checkAnterior());
    this.form.controls.mes.valueChanges.subscribe(() => this.checkAnterior());
    this.form.controls.ano.valueChanges.subscribe(() => this.checkAnterior());
  }

  createNova(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) {
      this.error.set('Preencha os campos obrigatórios.');
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api
      .create({
        setorId: value.setorId,
        mes: Number(value.mes),
        ano: Number(value.ano),
        tipoFuncionamento: value.tipoFuncionamento,
        observacao: value.observacao.trim() || null,
      })
      .subscribe({
        next: (escala) => void this.router.navigateByUrl(`/escalas/${escala.id}/editar`),
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível criar a escala.');
          this.saving.set(false);
        },
      });
  }

  copiarAnterior(): void {
    const anterior = this.escalaAnterior();
    if (!anterior) return;

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.error.set(null);
    this.api
      .copiar(anterior.id, {
        mes: Number(value.mes),
        ano: Number(value.ano),
        sobrescreverManuais: false,
      })
      .subscribe({
        next: (escala) => void this.router.navigateByUrl(`/escalas/${escala.id}/editar`),
        error: (err: { error?: { message?: string } }) => {
          this.error.set(err.error?.message ?? 'Não foi possível copiar a escala anterior.');
          this.saving.set(false);
        },
      });
  }

  save(): void {
    if (this.escalaAnterior()) {
      this.error.set('Escolha copiar a escala anterior ou criar uma nova.');
      return;
    }
    this.createNova();
  }

  cancel(): void {
    void this.router.navigateByUrl('/escalas');
  }

  private checkAnterior(): void {
    const setorId = this.form.controls.setorId.value;
    const mes = Number(this.form.controls.mes.value);
    const ano = Number(this.form.controls.ano.value);
    if (!setorId || !mes || !ano) {
      this.escalaAnterior.set(null);
      return;
    }

    this.api.getEscalaAnterior(setorId, ano, mes).subscribe({
      next: (info) => this.escalaAnterior.set(info),
      error: () => this.escalaAnterior.set(null),
    });
  }
}
